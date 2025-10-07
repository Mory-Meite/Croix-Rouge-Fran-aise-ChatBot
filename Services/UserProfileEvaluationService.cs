using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using InterviewChatbot.Models;
using System.Linq;

namespace InterviewChatbot.Services
{
    public class UserProfileEvaluationService
    {
        private readonly OpenAIService _openAIService;
        private readonly LoggingService _loggingService;
        
        // Questions d'évaluation des vulnérabilités
        private readonly List<string> _evaluationQuestions = new List<string>
        {
            "Pour commencer, quel est ton niveau de familiarité avec les entretiens d'embauche ? Est-ce une expérience nouvelle pour toi ou en as-tu déjà passé ?",
            "Y a-t-il des aspects spécifiques des entretiens qui t'inquiètent le plus en ce moment ? (Par exemple : parler de toi, répondre à des questions difficiles, gérer le stress ?)",
            "As-tu déjà rencontré des obstacles ou des difficultés particulières lors de tes recherches d'emploi ou d'entretiens précédents ?",
            "Te sens-tu à l'aise pour parler de tes expériences professionnelles ou de ton parcours jusqu'à présent ? Y a-t-il des périodes que tu préférerais aborder avec prudence ?",
            "Y a-t-il des aménagements ou des besoins spécifiques dont tu aimerais que nous tenions compte pour rendre cette préparation efficace pour toi ?",
            "Comment te sens-tu généralement face à l'idée de te \"vendre\" ou de mettre en avant tes compétences et qualités ?",
            "As-tu des appréhensions concernant les questions sur d'éventuelles \"lacunes\" dans ton parcours (périodes sans emploi, changements fréquents) ?",
            "Préfères-tu t'entraîner sur des situations très concrètes et réalistes, ou plutôt des scénarios progressifs et plus simples au début ?"
        };

        public UserProfileEvaluationService(OpenAIService openAIService, LoggingService loggingService)
        {
            _openAIService = openAIService;
            _loggingService = loggingService;
        }

        public async Task<Activity> StartEvaluationAsync(ITurnContext turnContext, UserProfile userProfile)
        {
            _loggingService.LogStageTransition(userProfile.UserId, "Menu Principal", "Évaluation");
            
            // Enregistrer que nous sommes en mode évaluation
            userProfile.CurrentState = "evaluation";
            userProfile.EvaluationStep = 0;
            userProfile.VulnerabilityProfile = new Dictionary<string, string>();
            
            // Message d'introduction
            string introMessage = "Bienvenue dans l'évaluation de profil ! Je vais te poser quelques questions pour mieux comprendre tes besoins et adapter nos sessions d'entraînement. Tu peux répondre simplement et honnêtement. Prêt à commencer ?";
            
            // Créer des boutons pour commencer ou sauter l'évaluation
            var reply = MessageFactory.Text(introMessage);
            reply.SuggestedActions = new SuggestedActions
            {
                Actions = new List<CardAction>
                {
                    new CardAction { Title = "✅ Commencer l'évaluation", Type = ActionTypes.ImBack, Value = "Commencer l'évaluation" },
                    new CardAction { Title = "⏩ Passer cette étape", Type = ActionTypes.ImBack, Value = "Passer l'évaluation" }
                }
            };

            return reply;
        }

        public async Task<Activity> ProcessEvaluationStepAsync(ITurnContext turnContext, UserProfile userProfile, string userMessage)
        {
            if (userMessage.Contains("Passer l'évaluation"))
            {
                return await CompleteEvaluationAsync(turnContext, userProfile);
            }

            // Si c'est le début ou si l'utilisateur confirme le démarrage
            if (userProfile.EvaluationStep == 0 && (userMessage.Contains("Commencer") || userMessage.Contains("Prêt")))
            {
                userProfile.EvaluationStep = 1;
                return MessageFactory.Text(_evaluationQuestions[0]);
            }
            
            // Enregistrer la réponse précédente
            if (userProfile.EvaluationStep > 0 && userProfile.EvaluationStep <= _evaluationQuestions.Count)
            {
                string questionKey = $"Question{userProfile.EvaluationStep}";
                userProfile.VulnerabilityProfile[questionKey] = userMessage;
                
                _loggingService.LogUserInteraction(userProfile.UserId, userMessage, "Réponse enregistrée", "Évaluation");
            }
            
            // Passer à la question suivante ou terminer
            userProfile.EvaluationStep++;
            
            if (userProfile.EvaluationStep <= _evaluationQuestions.Count)
            {
                return MessageFactory.Text(_evaluationQuestions[userProfile.EvaluationStep - 1]);
            }
            else
            {
                return await CompleteEvaluationAsync(turnContext, userProfile);
            }
        }

        private async Task<Activity> CompleteEvaluationAsync(ITurnContext turnContext, UserProfile userProfile)
        {
            // Analyser les réponses pour créer un profil de vulnérabilité
            await AnalyzeUserProfileAsync(userProfile);
            
            // Changer l'état pour retourner au menu principal
            userProfile.CurrentState = "menu";
            
            string completionMessage = "Merci pour tes réponses ! J'ai maintenant une meilleure compréhension de tes besoins. ";
            
            if (userProfile.VulnerabilityProfile.Count > 0)
            {
                completionMessage += "J'ai adapté nos futures sessions selon ton profil.";
            }
            else
            {
                completionMessage += "Nous allons utiliser un profil standard pour commencer.";
            }
            
            var reply = MessageFactory.Text(completionMessage);
            reply.SuggestedActions = new SuggestedActions
            {
                Actions = new List<CardAction>
                {
                    new CardAction { Title = "📋 Commencer la préparation", Type = ActionTypes.ImBack, Value = "Commencer la préparation" },
                    new CardAction { Title = "💬 Simuler un entretien", Type = ActionTypes.ImBack, Value = "Simuler un entretien" },
                    new CardAction { Title = "❓ Conseils généraux", Type = ActionTypes.ImBack, Value = "Conseils généraux" }
                }
            };

            _loggingService.LogStageTransition(userProfile.UserId, "Évaluation", "Menu Principal");
            
            return reply;
        }

        private async Task AnalyzeUserProfileAsync(UserProfile userProfile)
        {
            if (userProfile.VulnerabilityProfile.Count == 0)
            {
                return; // Pas de données à analyser
            }

            try
            {
                // Construire le prompt pour GPT-4o
                string systemPrompt = "Basé sur les réponses suivantes à un questionnaire d'évaluation des besoins pour la préparation d'entretien, " +
                                "identifie les principales vulnérabilités et préférences de cette personne. " +
                                "Détermine: \n" +
                                "1. Niveau d'expérience en entretien (débutant, intermédiaire, avancé)\n" +
                                "2. Niveau d'anxiété (faible, moyen, élevé)\n" +
                                "3. Besoins d'adaptation spécifiques\n" +
                                "4. Format d'apprentissage préféré\n" +
                                "5. Résume en une phrase le profil de cette personne";

                // Créer les messages
                var messages = new List<MessageDto>();
                string userPrompt = "Voici les réponses au questionnaire d'évaluation:\n\n";

                foreach (var item in userProfile.VulnerabilityProfile)
                {
                    int questionIndex = int.Parse(item.Key.Replace("Question", ""));
                    if (questionIndex > 0 && questionIndex <= _evaluationQuestions.Count)
                    {
                        userPrompt += $"Question: {_evaluationQuestions[questionIndex - 1]}\n";
                        userPrompt += $"Réponse: {item.Value}\n\n";
                    }
                }

                messages.Add(new MessageDto { Role = "user", Content = userPrompt });
                
                // Appeler OpenAI pour analyser le profil
                string analysis = await _openAIService.GetInterviewResponse(messages, systemPrompt);
                
                // Enregistrer l'analyse
                userProfile.VulnerabilityProfile["Analysis"] = analysis;
                
                // Extraire les informations principales pour le profil
                userProfile.ExperienceLevel = ExtractProfileData(analysis, "Niveau d'expérience");
                userProfile.AnxietyLevel = ExtractProfileData(analysis, "Niveau d'anxiété");
                userProfile.LearningPreference = ExtractProfileData(analysis, "Format d'apprentissage");
                userProfile.SpecificNeeds = ExtractProfileData(analysis, "Besoins d'adaptation");
                userProfile.IsProfileEvaluated = true;
                
                _loggingService.LogUserInteraction(userProfile.UserId, "Analyse du profil", $"Expérience={userProfile.ExperienceLevel}, Anxiété={userProfile.AnxietyLevel}", "Évaluation");
            }
            catch (Exception ex)
            {
                _loggingService.LogError(userProfile.UserId, $"Erreur lors de l'analyse du profil: {ex.Message}", ex);
            }
        }

        private string ExtractProfileData(string analysis, string dataType)
        {
            // Méthode simplifiée pour extraire des données de l'analyse
            if (string.IsNullOrEmpty(analysis)) return "standard";
            
            int index = analysis.IndexOf(dataType, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return "standard";
            
            int lineEnd = analysis.IndexOf('\n', index);
            if (lineEnd < 0) lineEnd = analysis.Length;
            
            string line = analysis.Substring(index, lineEnd - index);
            int colonIndex = line.IndexOf(':');
            
            if (colonIndex < 0) return "standard";
            
            return line.Substring(colonIndex + 1).Trim();
        }
    }
} 