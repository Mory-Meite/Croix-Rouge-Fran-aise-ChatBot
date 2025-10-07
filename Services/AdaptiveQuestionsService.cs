using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using InterviewChatbot.Models;
using Microsoft.Extensions.Logging;

namespace InterviewChatbot.Services
{
    /// <summary>
    /// Service qui adapte les questions d'entretien en fonction du profil de vulnérabilité de l'utilisateur
    /// </summary>
    public class AdaptiveQuestionsService
    {
        private readonly OpenAIService _openAIService;
        private readonly ILogger<AdaptiveQuestionsService> _logger;
        private readonly Dictionary<string, List<InterviewQuestion>> _questionBank;
        private readonly LoggingService _loggingService;
        
        // Catégories de questions
        private readonly List<string> _questionCategories = new List<string>
        {
            "introduction",       // Questions de présentation
            "experience",         // Questions sur l'expérience professionnelle
            "competences",        // Questions sur les compétences techniques/personnelles
            "motivation",         // Questions sur la motivation et le projet professionnel
            "situations",         // Questions de mise en situation / comportementales
            "lacunes",            // Questions sur les périodes d'inactivité ou échecs
            "aspirations",        // Questions sur les aspirations et objectifs futurs
            "conclusion"          // Questions de conclusion d'entretien
        };

        public AdaptiveQuestionsService(OpenAIService openAIService, ILogger<AdaptiveQuestionsService> logger, LoggingService loggingService)
        {
            _openAIService = openAIService;
            _logger = logger;
            _loggingService = loggingService;
            _questionBank = LoadQuestionBank();
        }

        /// <summary>
        /// Charge la banque de questions à partir du fichier ou crée une banque de questions par défaut
        /// </summary>
        private Dictionary<string, List<InterviewQuestion>> LoadQuestionBank()
        {
            var questionBank = new Dictionary<string, List<InterviewQuestion>>();
            
            // Initialiser chaque catégorie avec une liste vide
            foreach (var category in _questionCategories)
            {
                questionBank[category] = new List<InterviewQuestion>();
            }
            
            try
            {
                // À terme, charger d'une source de données plus structurée
                string questionContent = File.ReadAllText("Questions_Reponses.txt");
                
                // Pour l'instant, on utilise le service OpenAI pour générer des questions adaptées
                // car le fichier Questions_Reponses.txt n'est pas structuré pour être facilement parsé
                
                _logger.LogInformation("Banque de questions chargée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du chargement de la banque de questions: {ex.Message}");
            }
            
            return questionBank;
        }
        
        /// <summary>
        /// Obtient une question adaptée au profil de l'utilisateur et à la catégorie spécifiée
        /// </summary>
        public async Task<InterviewQuestion> GetAdaptedQuestionAsync(UserProfile profile, string category)
        {
            try
            {
                _loggingService.LogUserInteraction(profile.UserId, $"Demande de question adaptée", $"Catégorie: {category}", "AdaptiveQuestions");
                
                // Déterminer le niveau de difficulté en fonction du profil
                string difficulty = DetermineDifficultyLevel(profile);
                
                // Vérifier si nous avons des questions préexistantes pour cette catégorie et difficulté
                var matchingQuestions = _questionBank[category].Where(q => q.Difficulty == difficulty).ToList();
                
                // Si nous avons des questions correspondantes, en choisir une au hasard
                if (matchingQuestions.Count > 0)
                {
                    var random = new Random();
                    return matchingQuestions[random.Next(matchingQuestions.Count)];
                }
                
                // Sinon, générer une nouvelle question adaptée avec l'API OpenAI
                return await GenerateAdaptedQuestionAsync(profile, category, difficulty);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la récupération d'une question adaptée: {ex.Message}");
                
                // Retourner une question par défaut en cas d'erreur
                return new InterviewQuestion
                {
                    Id = Guid.NewGuid().ToString(),
                    QuestionText = $"Pouvez-vous me parler de votre expérience professionnelle dans le domaine {profile.JobSector}?",
                    Category = category,
                    Difficulty = "Moyen",
                    TipsForAnswer = "Soyez concis et mettez en avant vos principales réalisations."
                };
            }
        }
        
        /// <summary>
        /// Détermine le niveau de difficulté approprié en fonction du profil de l'utilisateur
        /// </summary>
        private string DetermineDifficultyLevel(UserProfile profile)
        {
            // Si le profil a été évalué, utiliser ces informations pour déterminer la difficulté
            if (profile.IsProfileEvaluated)
            {
                // Adaptation en fonction du niveau d'anxiété et d'expérience
                if (profile.AnxietyLevel.Contains("élevé") || profile.ExperienceLevel.Contains("débutant"))
                {
                    return "Facile";
                }
                else if (profile.AnxietyLevel.Contains("moyen") || profile.ExperienceLevel.Contains("intermédiaire"))
                {
                    return "Moyen";
                }
                else
                {
                    return "Difficile";
                }
            }
            
            // Par défaut, commencer avec un niveau moyen
            return "Moyen";
        }
        
        /// <summary>
        /// Génère une question adaptée en utilisant l'API OpenAI
        /// </summary>
        private async Task<InterviewQuestion> GenerateAdaptedQuestionAsync(UserProfile profile, string category, string difficulty)
        {
            // Créer un prompt spécifique pour générer une question adaptée
            string systemPrompt = CreateAdaptiveQuestionPrompt(profile, category, difficulty);
            
            // Construire les messages pour l'API
            var messages = new List<MessageDto>
            {
                new MessageDto { Role = "user", Content = $"Génère une question d'entretien de type {category} adaptée à mon profil." }
            };
            
            // Obtenir la réponse de l'API
            string response = await _openAIService.GetInterviewResponse(messages, systemPrompt);
            
            // Parser la réponse pour extraire la question et les conseils
            (string question, string tips) = ParseQuestionResponse(response);
            
            // Créer et retourner l'objet InterviewQuestion
            var interviewQuestion = new InterviewQuestion
            {
                Id = Guid.NewGuid().ToString(),
                QuestionText = question,
                Category = category,
                Difficulty = difficulty,
                TipsForAnswer = tips,
                JobSectors = new List<string> { profile.JobSector }
            };
            
            // Ajouter la question à la banque pour une utilisation future
            _questionBank[category].Add(interviewQuestion);
            
            return interviewQuestion;
        }
        
        /// <summary>
        /// Crée un prompt adapté pour générer une question pertinente
        /// </summary>
        private string CreateAdaptiveQuestionPrompt(UserProfile profile, string category, string difficulty)
        {
            string vulnerabilityContext = "";
            
            // Ajouter des informations sur les vulnérabilités spécifiques si elles existent
            if (profile.IsProfileEvaluated)
            {
                vulnerabilityContext = $@"
Profil de vulnérabilité:
- Niveau d'expérience en entretien: {profile.ExperienceLevel}
- Niveau d'anxiété: {profile.AnxietyLevel}
- Format d'apprentissage préféré: {profile.LearningPreference}
- Besoins spécifiques: {profile.SpecificNeeds}";
            }
            
            // Construire le prompt complet
            return $@"Tu es un expert en recrutement qui génère des questions d'entretien adaptées au profil du candidat.

Catégorie de question: {category}
Niveau de difficulté: {difficulty}
Secteur professionnel: {profile.JobSector}
Expérience: {profile.Experience}
Niveau de langue: {profile.LanguageLevel}
{vulnerabilityContext}

Instructions:
1. Génère une question d'entretien professionnelle, réaliste et bienveillante adaptée à ce profil.
2. Pour une question de niveau 'Facile', utilise un langage simple et direct, pose une question concrète sans ambiguïté.
3. Pour une question de niveau 'Moyen', tu peux être plus nuancé mais toujours clair.
4. Pour une question de niveau 'Difficile', tu peux poser une question plus complexe ou qui demande plus de réflexion.
5. Fournis ensuite un conseil bref mais utile pour répondre à cette question.
6. Format attendu: 'QUESTION: [ta question ici] CONSEIL: [ton conseil ici]'";
        }
        
        /// <summary>
        /// Parse la réponse pour extraire la question et les conseils
        /// </summary>
        private (string question, string tips) ParseQuestionResponse(string response)
        {
            string question = "";
            string tips = "";
            
            try
            {
                // Extraire la question
                int questionStart = response.IndexOf("QUESTION:", StringComparison.OrdinalIgnoreCase);
                int tipsStart = response.IndexOf("CONSEIL:", StringComparison.OrdinalIgnoreCase);
                
                if (questionStart >= 0 && tipsStart > questionStart)
                {
                    question = response.Substring(questionStart + 9, tipsStart - questionStart - 9).Trim();
                    tips = response.Substring(tipsStart + 8).Trim();
                }
                else
                {
                    // Fallback: considérer que tout est la question
                    question = response.Trim();
                    tips = "Soyez concis et authentique dans votre réponse.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du parsing de la réponse: {ex.Message}");
                question = response.Trim();
                tips = "Soyez concis et authentique dans votre réponse.";
            }
            
            return (question, tips);
        }
        
        /// <summary>
        /// Génère une série de questions adaptées pour une simulation complète
        /// </summary>
        public async Task<List<InterviewQuestion>> GenerateAdaptedInterviewSequenceAsync(UserProfile profile, int questionCount)
        {
            var questions = new List<InterviewQuestion>();
            
            try
            {
                // Toujours commencer par une question d'introduction
                questions.Add(await GetAdaptedQuestionAsync(profile, "introduction"));
                
                // Déterminer quelles catégories utiliser en fonction du profil
                var categoriesToUse = DetermineRelevantCategories(profile);
                
                // Répartir le nombre de questions restantes entre les catégories pertinentes
                int remainingQuestions = questionCount - 1; // -1 pour la question d'intro déjà ajoutée
                
                while (remainingQuestions > 0 && categoriesToUse.Count > 0)
                {
                    // Sélectionner une catégorie au hasard parmi celles restantes
                    var random = new Random();
                    int index = random.Next(categoriesToUse.Count);
                    string category = categoriesToUse[index];
                    
                    // Obtenir une question adaptée pour cette catégorie
                    questions.Add(await GetAdaptedQuestionAsync(profile, category));
                    
                    // Supprimer la catégorie utilisée (pour éviter trop de répétitions)
                    categoriesToUse.RemoveAt(index);
                    
                    // Si on a utilisé toutes les catégories mais qu'il reste des questions à générer,
                    // recréer la liste de catégories (sauf introduction, qu'on ne veut qu'une fois)
                    if (categoriesToUse.Count == 0 && remainingQuestions > 0)
                    {
                        categoriesToUse = _questionCategories
                            .Where(c => c != "introduction")
                            .ToList();
                    }
                    
                    remainingQuestions--;
                }
                
                // Terminer par une question de conclusion si c'est une simulation complète
                if (questionCount >= 5 && !questions.Any(q => q.Category == "conclusion"))
                {
                    questions.Add(await GetAdaptedQuestionAsync(profile, "conclusion"));
                }
                
                return questions;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la génération de la séquence d'entretien: {ex.Message}");
                
                // En cas d'erreur, retourner au moins une question générique
                if (questions.Count == 0)
                {
                    questions.Add(new InterviewQuestion
                    {
                        Id = Guid.NewGuid().ToString(),
                        QuestionText = "Pouvez-vous vous présenter et me parler de votre parcours professionnel?",
                        Category = "introduction",
                        Difficulty = "Moyen",
                        TipsForAnswer = "Présentez-vous de manière concise en mettant en avant vos expériences pertinentes pour le poste."
                    });
                }
                
                return questions;
            }
        }
        
        /// <summary>
        /// Détermine les catégories de questions pertinentes en fonction du profil
        /// </summary>
        private List<string> DetermineRelevantCategories(UserProfile profile)
        {
            var relevantCategories = new List<string>();
            
            // Inclure les catégories de base qui sont toujours pertinentes
            relevantCategories.AddRange(new[] {
                "experience", 
                "competences", 
                "motivation"
            });
            
            // Ajuster en fonction du profil de vulnérabilité (si évalué)
            if (profile.IsProfileEvaluated)
            {
                // Si le niveau d'anxiété est élevé, éviter certaines questions difficiles au début
                if (profile.AnxietyLevel.Contains("élevé"))
                {
                    // Nous incluons quand même "lacunes" mais avec une difficulté adaptée
                    relevantCategories.Add("aspirations"); // Questions positives sur l'avenir
                }
                else
                {
                    relevantCategories.AddRange(new[] {
                        "situations",   // Questions comportementales
                        "lacunes",      // Questions sur les périodes difficiles
                        "aspirations"   // Questions sur les objectifs futurs
                    });
                }
                
                // Si l'utilisateur a indiqué des appréhensions spécifiques sur les lacunes
                if (profile.VulnerabilityProfile.ContainsKey("Question7") && 
                    profile.VulnerabilityProfile["Question7"].Contains("oui"))
                {
                    // Nous incluons quand même cette catégorie mais elle sera adaptée en difficulté
                    if (!relevantCategories.Contains("lacunes"))
                    {
                        relevantCategories.Add("lacunes");
                    }
                }
            }
            else
            {
                // Si le profil n'a pas été évalué, inclure toutes les catégories sauf introduction
                relevantCategories.AddRange(_questionCategories.Where(c => c != "introduction" && c != "conclusion"));
            }
            
            return relevantCategories;
        }
    }
} 