using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using InterviewChatbot.Models;
using Microsoft.Extensions.Logging;

namespace InterviewChatbot.Services
{
    public class InterviewDialogService
    {
        private readonly OpenAIService _openAIService;
        private readonly ILogger<InterviewDialogService> _logger;
        private readonly string _knowledgeBase;

        public InterviewDialogService(OpenAIService openAIService, ILogger<InterviewDialogService> logger)
        {
            _openAIService = openAIService;
            _logger = logger;
            
            // Charger la base de connaissances
            try 
            {
                _knowledgeBase = File.ReadAllText("Guide_Embauche.markdown");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du chargement de la base de connaissances: {ex.Message}");
                _knowledgeBase = "Guide non disponible";
            }
        }

        /// <summary>
        /// Génère un prompt système pour guider le comportement de l'IA
        /// </summary>
        private string GetSystemPrompt(InterviewStage stage, UserProfile userProfile)
        {
            string basePrompt = $@"Tu es un assistant d'entretien d'embauche bienveillant et encourageant qui aide des personnes vulnérables à se préparer pour des entretiens d'embauche. 
Utilise ces informations comme base de connaissances:
{_knowledgeBase}

Profil de l'utilisateur:
- Niveau de langage: {userProfile.LanguageLevel}
- Secteur recherché: {userProfile.JobSector}
- Expérience: {userProfile.Experience}
- Niveau d'aisance numérique: {userProfile.DigitalSkillLevel}

Instructions importantes:
- Utilise un langage simple et accessible
- Sois encourageant et bienveillant, jamais critique
- Donne des exemples concrets et pratiques
- Pose une question à la fois
- Adapte le niveau de difficulté au profil de l'utilisateur
";

            switch (stage)
            {
                case InterviewStage.Introduction:
                    return basePrompt + @"
Tu es dans la phase d'INTRODUCTION. Présente-toi comme un assistant d'entretien, explique brièvement ce qu'est un entretien d'embauche et demande à l'utilisateur s'il a déjà passé des entretiens avant. Sois chaleureux et rassurant.";

                case InterviewStage.Preparation:
                    return basePrompt + @"
Tu es dans la phase de PRÉPARATION. Explique les étapes clés d'un entretien d'embauche et donne des conseils pour se préparer (rechercher l'entreprise, préparer des réponses, questions à poser, etc.).";

                case InterviewStage.Simulation:
                    return basePrompt + @"
Tu es dans la phase de SIMULATION D'ENTRETIEN. Tu joues le rôle d'un recruteur. Pose des questions d'entretien adaptées au profil de l'utilisateur et au secteur qu'il recherche. Après chaque réponse de l'utilisateur, donne un feedback constructif et bienveillant.";

                case InterviewStage.Feedback:
                    return basePrompt + @"
Tu es dans la phase de FEEDBACK. Résume les points forts et les points à améliorer de l'utilisateur basés sur ses réponses précédentes. Propose des conseils pratiques et des exercices pour progresser.";

                default:
                    return basePrompt;
            }
        }

        /// <summary>
        /// Traite un message utilisateur selon l'étape de l'entretien
        /// </summary>
        public async Task<string> ProcessUserMessage(string userMessage, List<MessageDto> conversationHistory, InterviewStage stage, UserProfile userProfile)
        {
            try
            {
                // Ajouter le message de l'utilisateur à l'historique
                conversationHistory.Add(new MessageDto { Role = "user", Content = userMessage });
                
                // Obtenir le prompt système adapté à l'étape et au profil
                string systemPrompt = GetSystemPrompt(stage, userProfile);
                
                // Obtenir la réponse de l'IA
                string response = await _openAIService.GetInterviewResponse(conversationHistory, systemPrompt);
                
                // Ajouter la réponse à l'historique
                conversationHistory.Add(new MessageDto { Role = "assistant", Content = response });
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du traitement du message: {ex.Message}");
                return "Désolé, j'ai rencontré un problème lors du traitement de votre message. Pouvez-vous reformuler ou essayer plus tard?";
            }
        }
        
        /// <summary>
        /// Obtient une réponse de l'IA pour une simulation d'entretien
        /// </summary>
        public async Task<string> GetInterviewResponse(List<MessageDto> messages, string prompt)
        {
            try
            {
                return await _openAIService.GetInterviewResponse(messages, prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors de la génération de la réponse: {ex.Message}");
                return "Désolé, j'ai rencontré un problème pour générer une réponse. Pouvez-vous réessayer?";
            }
        }
        
        /// <summary>
        /// Détermine si l'utilisateur est prêt à passer à l'étape suivante
        /// </summary>
        public async Task<bool> ShouldAdvanceToNextStage(List<MessageDto> conversationHistory, InterviewStage currentStage, UserProfile userProfile)
        {
            // Créer un prompt pour demander à l'IA si l'utilisateur est prêt pour la prochaine étape
            string systemPrompt = $@"Tu es un assistant qui évalue si l'utilisateur est prêt à passer à la prochaine étape d'un entretien simulé.
Étape actuelle: {currentStage}
Réponds uniquement par OUI ou NON.";

            var evaluationMessages = new List<MessageDto>(conversationHistory)
            {
                new MessageDto 
                { 
                    Role = "user", 
                    Content = "Basé sur notre conversation, suis-je prêt à passer à l'étape suivante de la préparation à l'entretien?" 
                }
            };

            string response = await _openAIService.GetInterviewResponse(evaluationMessages, systemPrompt);
            
            return response.ToUpper().Contains("OUI");
        }
    }
} 