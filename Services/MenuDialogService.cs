using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InterviewChatbot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace InterviewChatbot.Services
{
    public class MenuDialogService
    {
        private readonly OpenAIService _openAIService;
        private readonly LoggingService _loggingService;
        private readonly ILogger<MenuDialogService> _logger;
        private readonly UserProfileEvaluationService _evaluationService;
        private readonly AdaptiveQuestionsService _adaptiveQuestionsService;

        public MenuDialogService(
            OpenAIService openAIService, 
            LoggingService loggingService, 
            ILogger<MenuDialogService> logger,
            UserProfileEvaluationService evaluationService,
            AdaptiveQuestionsService adaptiveQuestionsService)
        {
            _openAIService = openAIService;
            _loggingService = loggingService;
            _logger = logger;
            _evaluationService = evaluationService;
            _adaptiveQuestionsService = adaptiveQuestionsService;
        }

        /// <summary>
        /// Traite le message de l'utilisateur en fonction de l'état actuel de l'interaction
        /// </summary>
        public async Task<Activity> ProcessUserMessageAsync(ITurnContext turnContext, string userMessage, UserProfile userProfile)
        {
            // Si l'utilisateur est en mode évaluation
            if (userProfile.CurrentState == "evaluation")
            {
                return await _evaluationService.ProcessEvaluationStepAsync(turnContext, userProfile, userMessage);
            }
            
            // Autres traitements basés sur l'état...
            
            // Par défaut, retourne le menu principal
            return InteractiveElements.CreateMainMenu();
        }

        /// <summary>
        /// Traite la sélection dans le menu principal
        /// </summary>
        public async Task<(string Message, Activity Options)> ProcessMainMenuSelection(string selection, InterviewSession session)
        {
            switch (selection)
            {
                case "👤 Évaluer mon profil":
                    session.UserProfile.CurrentState = "evaluation";
                    _loggingService.LogStageTransition(session.UserProfile.UserId, "MenuPrincipal", "Évaluation");
                    
                    // On utilise ITurnContext.Activity.GetConversationReference() pour obtenir la référence de conversation
                    // Pour cette implémentation, nous devons passer par une méthode asynchrone séparée
                    var message = await StartProfileEvaluationAsync(session.UserProfile);
                    return (message.Text, null); // Retourne le message sans options supplémentaires
                
                case "📋 Commencer la préparation":
                    // Adapter la préparation en fonction du profil de vulnérabilité si évalué
                    if (session.UserProfile.IsProfileEvaluated)
                    {
                        string adaptedMessage = "Préparons-nous pour votre entretien ! ";
                        
                        // Adapter le message en fonction du niveau d'anxiété
                        if (session.UserProfile.AnxietyLevel == "élevé")
                        {
                            adaptedMessage += "Ne vous inquiétez pas, nous allons procéder étape par étape, à votre rythme. ";
                        }
                        
                        // Adapter le message en fonction du niveau d'expérience
                        if (session.UserProfile.ExperienceLevel == "débutant")
                        {
                            adaptedMessage += "Nous commencerons par les bases pour vous mettre en confiance. ";
                        }
                        
                        adaptedMessage += "Voici quelques options pour vous aider :";
                        
                        session.CurrentStage = InterviewStage.Preparation;
                        _loggingService.LogStageTransition(session.UserProfile.UserId, "MenuPrincipal", "Preparation");
                        return (adaptedMessage, InteractiveElements.CreatePreparationMenu());
                    }
                    else
                    {
                        session.CurrentStage = InterviewStage.Preparation;
                        _loggingService.LogStageTransition(session.UserProfile.UserId, "MenuPrincipal", "Preparation");
                        return ("Préparons-nous pour votre entretien ! Voici quelques options pour vous aider :", 
                                InteractiveElements.CreatePreparationMenu());
                    }

                case "💬 Simuler un entretien":
                    // Adapter la simulation en fonction du profil de vulnérabilité si évalué
                    if (session.UserProfile.IsProfileEvaluated)
                    {
                        string adaptedMessage = "C'est parti pour une simulation d'entretien ! ";
                        
                        // Adapter en fonction de la préférence d'apprentissage
                        if (session.UserProfile.LearningPreference == "progressif")
                        {
                            adaptedMessage += "Je vous propose de commencer par une simulation progressive avec des questions de difficulté croissante. ";
                        }
                        else if (session.UserProfile.LearningPreference == "réaliste")
                        {
                            adaptedMessage += "Nous allons faire une simulation aussi réaliste que possible, pour vous préparer aux conditions réelles. ";
                        }
                        
                        adaptedMessage += "Quel type de simulation souhaitez-vous ?";
                        
                        session.CurrentStage = InterviewStage.Simulation;
                        _loggingService.LogStageTransition(session.UserProfile.UserId, "MenuPrincipal", "Simulation");
                        return (adaptedMessage, InteractiveElements.CreateSimulationMenu());
                    }
                    else
                    {
                        session.CurrentStage = InterviewStage.Simulation;
                        _loggingService.LogStageTransition(session.UserProfile.UserId, "MenuPrincipal", "Simulation");
                        return ("C'est parti pour une simulation d'entretien ! Quel type de simulation souhaitez-vous ?", 
                                InteractiveElements.CreateSimulationMenu());
                    }

                case "📊 Voir mon feedback":
                    session.CurrentStage = InterviewStage.Feedback;
                    _loggingService.LogStageTransition(session.UserProfile.UserId, "MenuPrincipal", "Feedback");
                    
                    // Génération de feedback si l'historique est suffisant
                    if (session.Feedback.Count > 0 || session.ConversationHistory.Count > 5)
                    {
                        string feedback = await GenerateFeedbackSummary(session);
                        return (feedback, CreateFeedbackOptions());
                    }
                    else
                    {
                        return ("Vous n'avez pas encore assez d'historique pour un feedback détaillé. Essayez d'abord une simulation d'entretien !", 
                                InteractiveElements.CreateMainMenu());
                    }

                case "❓ Conseils généraux":
                    return (await GetGeneralAdvice(session.UserProfile), 
                            InteractiveElements.CreateMainMenu());

                case "⚙️ Mettre à jour mon profil":
                    return ("Mettons à jour votre profil. Qu'aimeriez-vous modifier ?", 
                            CreateProfileUpdateOptions());

                default:
                    return ("Je n'ai pas compris votre sélection. Voici le menu principal :", 
                            InteractiveElements.CreateMainMenu());
            }
        }

        /// <summary>
        /// Traite la sélection dans le menu de préparation
        /// </summary>
        public async Task<(string Message, Activity Options)> ProcessPreparationMenuSelection(string selection, InterviewSession session)
        {
            switch (selection)
            {
                case "🔍 Rechercher l'entreprise":
                    string prompt = "Tu es un conseiller qui explique comment bien rechercher une entreprise avant un entretien. " +
                                    "Donne des conseils pratiques sur les aspects à rechercher et où trouver ces informations.";
                    
                    var messages = new List<MessageDto>
                    {
                        new MessageDto { Role = "user", Content = "Comment rechercher efficacement des informations sur une entreprise avant un entretien ?" }
                    };
                    
                    string response = await _openAIService.GetInterviewResponse(messages, prompt);
                    return (response, CreateResearchFollowUpOptions());

                case "🗣️ Questions fréquentes":
                    return (await GetFrequentlyAskedQuestions(session.UserProfile.JobSector), 
                            CreateFAQFollowUpOptions());

                case "👔 Conseils de présentation":
                    string presentationPrompt = "Tu es un conseiller en image professionnelle qui explique comment se présenter " +
                                               "pour un entretien d'embauche (tenue vestimentaire, langage corporel, etc.). " +
                                               "Donne des conseils adaptés au secteur suivant : " + session.UserProfile.JobSector;
                    
                    var presentationMessages = new List<MessageDto>
                    {
                        new MessageDto { Role = "user", Content = "Comment me présenter lors d'un entretien d'embauche ?" }
                    };
                    
                    string presentationResponse = await _openAIService.GetInterviewResponse(presentationMessages, presentationPrompt);
                    return (presentationResponse, InteractiveElements.CreatePreparationMenu());

                case "📝 Préparer mes réponses":
                    return ("Préparons vos réponses aux questions typiques. Quel type de question souhaitez-vous préparer ?", 
                            CreateQuestionTypeOptions());

                case "🏠 Retour au menu principal":
                    return ("Voici le menu principal :", 
                            InteractiveElements.CreateMainMenu());

                default:
                    return ("Je n'ai pas compris votre sélection. Voici les options de préparation :", 
                            InteractiveElements.CreatePreparationMenu());
            }
        }

        /// <summary>
        /// Traite la sélection dans le menu de simulation
        /// </summary>
        public async Task<(string Message, Activity Options)> ProcessSimulationMenuSelection(string selection, InterviewSession session)
        {
            switch (selection)
            {
                case "🚀 Démarrer la simulation":
                    string simulationIntro = "Je vais maintenant jouer le rôle d'un recruteur pour une simulation d'entretien. " +
                                            "Je vais vous poser des questions et vous donner un feedback sur vos réponses. " +
                                            "Commençons !";
                    
                    session.ConversationHistory.Add(new MessageDto { Role = "assistant", Content = simulationIntro });
                    
                    // Utiliser le service de questions adaptatives pour générer la première question
                    var firstQuestionObj = await _adaptiveQuestionsService.GetAdaptedQuestionAsync(session.UserProfile, "introduction");
                    string firstQuestion = firstQuestionObj.QuestionText;
                    
                    // Stocker le conseil dans les métadonnées de la session pour l'utiliser lors du feedback
                    session.Feedback["dernière_question_conseil"] = firstQuestionObj.TipsForAnswer;
                    
                    session.ConversationHistory.Add(new MessageDto { Role = "assistant", Content = firstQuestion });
                    
                    return (simulationIntro + "\n\n" + firstQuestion, 
                            InteractiveElements.CreateSimulationResponseOptions());

                case "⏱️ Simulation courte (5-10 min)":
                    session.ConversationHistory.Add(new MessageDto { 
                        Role = "system", 
                        Content = "Configuration: simulation courte, 3-5 questions, niveau adapté: " + session.UserProfile.LanguageLevel 
                    });
                    
                    // Préparer une séquence de questions adaptées
                    var shortSimulationQuestions = await _adaptiveQuestionsService.GenerateAdaptedInterviewSequenceAsync(session.UserProfile, 3);
                    
                    // Stocker les questions dans la session pour y accéder plus tard
                    session.Feedback["questions_préparées"] = string.Join("|", shortSimulationQuestions.Select(q => q.Id));
                    
                    // Stocker également les questions dans un format accessible
                    foreach (var question in shortSimulationQuestions)
                    {
                        session.Feedback[$"question_{question.Id}"] = question.QuestionText;
                        session.Feedback[$"conseil_{question.Id}"] = question.TipsForAnswer;
                    }
                    
                    return ("Parfait, nous allons faire une simulation courte de 3 questions adaptées à votre profil. Prêt à commencer ?", 
                            CreateReadyOptions());

                case "⏳ Simulation complète (15-20 min)":
                    session.ConversationHistory.Add(new MessageDto { 
                        Role = "system", 
                        Content = "Configuration: simulation complète, 8-10 questions, niveau adapté: " + session.UserProfile.LanguageLevel 
                    });
                    
                    // Préparer une séquence plus longue de questions adaptées
                    var fullSimulationQuestions = await _adaptiveQuestionsService.GenerateAdaptedInterviewSequenceAsync(session.UserProfile, 8);
                    
                    // Stocker les questions dans la session pour y accéder plus tard
                    session.Feedback["questions_préparées"] = string.Join("|", fullSimulationQuestions.Select(q => q.Id));
                    
                    // Stocker également les questions dans un format accessible
                    foreach (var question in fullSimulationQuestions)
                    {
                        session.Feedback[$"question_{question.Id}"] = question.QuestionText;
                        session.Feedback[$"conseil_{question.Id}"] = question.TipsForAnswer;
                    }
                    
                    return ("Nous allons faire une simulation complète de 8 questions adaptées à votre profil. Prenez votre temps pour répondre. Prêt à commencer ?", 
                            CreateReadyOptions());

                case "🎮 Simulation par thème":
                    return ("Quel aspect spécifique de l'entretien souhaitez-vous simuler ?", 
                            CreateSimulationThemeOptions());

                case "🏠 Retour au menu principal":
                    return ("Voici le menu principal :", 
                            InteractiveElements.CreateMainMenu());

                default:
                    return ("Je n'ai pas compris votre sélection. Voici les options de simulation :", 
                            InteractiveElements.CreateSimulationMenu());
            }
        }

        /// <summary>
        /// Obtient les questions fréquemment posées pour un secteur donné
        /// </summary>
        private async Task<string> GetFrequentlyAskedQuestions(string jobSector)
        {
            string prompt = $"Tu es un coach spécialisé dans la préparation aux entretiens d'embauche. " +
                           $"Liste les 5 questions les plus fréquemment posées lors d'un entretien d'embauche " +
                           $"dans le secteur {jobSector}. Pour chaque question, donne un conseil sur la façon d'y répondre efficacement.";
            
            var messages = new List<MessageDto>
            {
                new MessageDto { Role = "user", Content = $"Quelles sont les questions fréquentes en entretien dans le secteur {jobSector} ?" }
            };
            
            return await _openAIService.GetInterviewResponse(messages, prompt);
        }

        /// <summary>
        /// Obtient les conseils généraux pour les entretiens
        /// </summary>
        private async Task<string> GetGeneralAdvice(UserProfile profile)
        {
            string prompt = "Tu es un coach d'entretien d'embauche qui donne des conseils généraux pour réussir un entretien. " +
                           $"Adapte tes conseils au niveau de compétence de l'utilisateur (niveau de langue: {profile.LanguageLevel}, " +
                           $"niveau numérique: {profile.DigitalSkillLevel}). Sois concret et pratique.";
            
            var messages = new List<MessageDto>
            {
                new MessageDto { Role = "user", Content = "Donnez-moi des conseils généraux pour réussir un entretien d'embauche." }
            };
            
            return await _openAIService.GetInterviewResponse(messages, prompt);
        }

        /// <summary>
        /// Génère une question pour la simulation d'entretien
        /// </summary>
        private async Task<string> GetSimulationQuestion(UserProfile profile, string questionType)
        {
            // Utiliser le service de questions adaptatives
            var questionObj = await _adaptiveQuestionsService.GetAdaptedQuestionAsync(profile, questionType);
            return questionObj.QuestionText;
            
            // Code original commenté ci-dessous
            /*
            string prompt = $"Tu es un recruteur qui pose une question d'entretien de type '{questionType}' " +
                           $"à un candidat dans le secteur {profile.JobSector} avec un niveau d'expérience '{profile.Experience}'. " +
                           $"Pose uniquement la question, sans explication supplémentaire.";
            
            var messages = new List<MessageDto>
            {
                new MessageDto { Role = "user", Content = $"Posez-moi une question d'entretien de type {questionType}." }
            };
            
            return await _openAIService.GetInterviewResponse(messages, prompt);
            */
        }

        /// <summary>
        /// Génère un résumé de feedback basé sur l'historique des conversations
        /// </summary>
        private async Task<string> GenerateFeedbackSummary(InterviewSession session)
        {
            // Si nous avons des feedbacks enregistrés, les utiliser
            if (session.Feedback.Count > 0)
            {
                string feedbackSummary = "Voici un résumé de vos performances :\n\n";
                
                // Extraire les conseils liés aux questions adaptées
                var questionConseils = new Dictionary<string, string>();
                
                foreach (var feedback in session.Feedback)
                {
                    if (feedback.Key.StartsWith("conseil_"))
                    {
                        string questionId = feedback.Key.Substring(8); // Longueur de "conseil_"
                        if (session.Feedback.ContainsKey($"question_{questionId}"))
                        {
                            questionConseils[session.Feedback[$"question_{questionId}"]] = feedback.Value;
                        }
                    }
                }
                
                // Ajouter les feedbacks classiques
                foreach (var feedback in session.Feedback)
                {
                    // Ne pas afficher les métadonnées techniques
                    if (!feedback.Key.StartsWith("question_") && 
                        !feedback.Key.StartsWith("conseil_") && 
                        !feedback.Key.Equals("questions_préparées"))
                    {
                        feedbackSummary += $"• {feedback.Key} : {feedback.Value}\n";
                    }
                }
                
                // Ajouter les conseils liés aux questions si disponibles
                if (questionConseils.Count > 0)
                {
                    feedbackSummary += "\nConseils pour améliorer vos réponses :\n\n";
                    
                    foreach (var conseil in questionConseils)
                    {
                        feedbackSummary += $"• Pour la question \"{conseil.Key}\" : {conseil.Value}\n\n";
                    }
                }
                
                return feedbackSummary;
            }
            // Sinon, générer un feedback basé sur l'historique des conversations
            else
            {
                // Enrichir le prompt avec le profil de vulnérabilité de l'utilisateur
                string promptContexte = "";
                
                if (session.UserProfile.IsProfileEvaluated)
                {
                    promptContexte = $@"
Profil de vulnérabilité du candidat:
- Niveau d'expérience en entretien: {session.UserProfile.ExperienceLevel}
- Niveau d'anxiété: {session.UserProfile.AnxietyLevel}
- Format d'apprentissage préféré: {session.UserProfile.LearningPreference}
- Besoins spécifiques: {session.UserProfile.SpecificNeeds}

Adapte ton feedback en fonction de ce profil. Sois particulièrement bienveillant si le niveau d'anxiété est élevé,
et donne des conseils structurés et progressifs si le format d'apprentissage préféré est 'progressif'.";
                }
                
                string prompt = $@"Tu es un coach d'entretien qui analyse les performances d'un candidat en entretien.
Analyse l'historique de conversation ci-dessous et fournis un feedback constructif,
en soulignant les points forts et les points à améliorer. Sois bienveillant et encourageant.
{promptContexte}

Structure ton feedback en 3 parties:
1. Points forts observés
2. Axes d'amélioration
3. Conseils personnalisés pour progresser";
                
                // Prendre les derniers échanges (max 10) pour l'analyse
                var relevantHistory = session.ConversationHistory.Count <= 10 
                    ? session.ConversationHistory 
                    : session.ConversationHistory.GetRange(session.ConversationHistory.Count - 10, 10);
                
                var messages = new List<MessageDto>(relevantHistory)
                {
                    new MessageDto { Role = "user", Content = "Pouvez-vous me donner un feedback sur mes réponses en entretien ?" }
                };
                
                return await _openAIService.GetInterviewResponse(messages, prompt);
            }
        }

        /// <summary>
        /// Crée les options pour le suivi après recherche d'entreprise
        /// </summary>
        private Activity CreateResearchFollowUpOptions()
        {
            var options = new List<string>
            {
                "🌐 Sites à consulter",
                "📊 Données à rechercher",
                "📝 Prendre des notes",
                "🔙 Retour préparation",
                "🏠 Menu principal"
            };
            
            return InteractiveElements.CreateButtonMessage("Que voulez-vous faire ensuite ?", options);
        }

        /// <summary>
        /// Crée les options pour le suivi des questions fréquentes
        /// </summary>
        private Activity CreateFAQFollowUpOptions()
        {
            var options = new List<string>
            {
                "💼 Questions sur l'expérience",
                "🔧 Questions techniques",
                "🧠 Questions comportementales",
                "🔙 Retour préparation",
                "🏠 Menu principal"
            };
            
            return InteractiveElements.CreateButtonMessage("Quels types de questions vous intéressent ?", options);
        }

        /// <summary>
        /// Crée les options pour les types de questions à préparer
        /// </summary>
        private Activity CreateQuestionTypeOptions()
        {
            var options = new List<string>
            {
                "👋 Se présenter",
                "💼 Parcours professionnel",
                "💪 Forces et faiblesses",
                "🔮 Projets futurs",
                "🏠 Menu principal"
            };
            
            return InteractiveElements.CreateButtonMessage("Sur quel sujet voulez-vous préparer vos réponses ?", options);
        }

        /// <summary>
        /// Crée les options pour la mise à jour du profil
        /// </summary>
        private Activity CreateProfileUpdateOptions()
        {
            var options = new List<string>
            {
                "🏢 Secteur d'activité",
                "📊 Niveau d'expérience",
                "🗣️ Niveau de langue",
                "💻 Compétences numériques",
                "🏠 Menu principal"
            };
            
            return InteractiveElements.CreateButtonMessage("Que souhaitez-vous mettre à jour dans votre profil ?", options);
        }

        /// <summary>
        /// Crée les options de prêt à commencer
        /// </summary>
        private Activity CreateReadyOptions()
        {
            var options = new List<string>
            {
                "✅ Je suis prêt",
                "⏱️ Donnez-moi une minute",
                "❓ Quelques conseils avant",
                "🏠 Menu principal"
            };
            
            return InteractiveElements.CreateButtonMessage("Êtes-vous prêt à commencer ?", options);
        }

        /// <summary>
        /// Crée les options pour les thèmes de simulation
        /// </summary>
        private Activity CreateSimulationThemeOptions()
        {
            var options = new List<string>
            {
                "🤝 Début d'entretien",
                "💰 Négociation salariale",
                "❓ Questions difficiles",
                "🧠 Questions pièges",
                "🏠 Menu principal"
            };
            
            return InteractiveElements.CreateButtonMessage("Quel aspect spécifique voulez-vous simuler ?", options);
        }

        /// <summary>
        /// Crée les options après feedback
        /// </summary>
        private Activity CreateFeedbackOptions()
        {
            var options = new List<string>
            {
                "📊 Feedback détaillé",
                "📝 Conseils d'amélioration",
                "🔄 Nouvelle simulation",
                "🏠 Menu principal"
            };
            
            return InteractiveElements.CreateButtonMessage("Que souhaitez-vous faire avec ce feedback ?", options);
        }

        /// <summary>
        /// Démarre l'évaluation du profil utilisateur
        /// </summary>
        private async Task<Activity> StartProfileEvaluationAsync(UserProfile userProfile)
        {
            // Note: Dans une implémentation réelle avec le SDK Bot Framework,
            // nous aurions besoin d'un ITurnContext valide. Cette méthode est 
            // une simplification pour notre démo.
            try 
            {
                // Nous passons null pour le TurnContext - ce n'est pas idéal mais 
                // notre service d'évaluation peut fonctionner sans un TurnContext complet
                // pour cette démonstration
                return await _evaluationService.StartEvaluationAsync(null, userProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du démarrage de l'évaluation: {ex.Message}");
                return MessageFactory.Text("Désolé, une erreur s'est produite lors de l'évaluation du profil. Veuillez réessayer.");
            }
        }
    }
} 