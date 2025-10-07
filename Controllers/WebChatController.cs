using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InterviewChatbot.Models;
using InterviewChatbot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace InterviewChatbot.Controllers
{
    [Route("api/webchat")]
    [ApiController]
    public class WebChatController : ControllerBase
    {
        private readonly IBot _bot;
        private readonly InterviewDialogService _interviewDialogService;
        private readonly MenuDialogService _menuDialogService;
        private readonly LoggingService _loggingService;

        public WebChatController(
            IBot bot, 
            InterviewDialogService interviewDialogService,
            MenuDialogService menuDialogService,
            LoggingService loggingService)
        {
            _bot = bot;
            _interviewDialogService = interviewDialogService;
            _menuDialogService = menuDialogService;
            _loggingService = loggingService;
        }

        public class WebChatRequest
        {
            public string Text { get; set; }
            public string UserId { get; set; }
        }

        public class WebChatResponse
        {
            public string Text { get; set; }
            public SuggestedActions SuggestedActions { get; set; }
        }

        [HttpPost("messages")]
        public async Task<IActionResult> PostMessage([FromBody] WebChatRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Text))
                {
                    return BadRequest(new { error = "Le message ne peut pas être vide" });
                }

                string userId = request.UserId ?? "web_user";
                string userMessage = request.Text;

                // Journaliser le message reçu
                _loggingService.LogUserInteraction(userId, userMessage, "Réponse non encore générée", "WebChat");

                // Créer ou récupérer une session pour cet utilisateur
                var session = InterviewSession.GetOrCreateSession(userId);
                
                // Ajouter le message à l'historique
                session.AddUserMessage(userMessage);

                // Traiter le message
                string responseText = "";
                List<string> options = new List<string>();

                // Vérifier les commandes spéciales
                if (userMessage.Contains("Menu principal") || userMessage == "🏠")
                {
                    responseText = "Voici le menu principal :";
                    options = new List<string> {
                        "📋 Commencer la préparation",
                        "💬 Simuler un entretien",
                        "📊 Voir mon feedback",
                        "❓ Conseils généraux",
                        "⚙️ Mettre à jour mon profil"
                    };
                }
                // Menu principal - options
                else if (userMessage.StartsWith("📋") || 
                    userMessage.StartsWith("💬") || 
                    userMessage.StartsWith("📊") || 
                    userMessage.StartsWith("❓") || 
                    userMessage.StartsWith("⚙️"))
                {
                    var (responseMessage, _) = await _menuDialogService.ProcessMainMenuSelection(userMessage, session);
                    responseText = responseMessage;
                    
                    // Déterminer les options en fonction de la sélection
                    if (userMessage.StartsWith("📋")) // Préparation
                    {
                        options = new List<string> {
                            "🔍 Rechercher l'entreprise",
                            "🗣️ Questions fréquentes",
                            "👔 Conseils de présentation",
                            "📝 Préparer mes réponses",
                            "🏠 Retour au menu principal"
                        };
                    }
                    else if (userMessage.StartsWith("💬")) // Simulation
                    {
                        options = new List<string> {
                            "🚀 Démarrer la simulation",
                            "⏱️ Simulation courte (5-10 min)",
                            "⏳ Simulation complète (15-20 min)",
                            "🎮 Simulation par thème",
                            "🏠 Retour au menu principal"
                        };
                    }
                    else if (userMessage.StartsWith("📊")) // Feedback
                    {
                        options = new List<string> {
                            "💬 Simuler un entretien",
                            "🏠 Retour au menu principal"
                        };
                    }
                    else if (userMessage.StartsWith("❓")) // Conseils
                    {
                        options = new List<string> {
                            "📋 Commencer la préparation",
                            "💬 Simuler un entretien",
                            "🏠 Retour au menu principal"
                        };
                    }
                    else if (userMessage.StartsWith("⚙️")) // Profil
                    {
                        options = new List<string> {
                            "🏢 Secteur d'activité",
                            "📊 Niveau d'expérience",
                            "🗣️ Niveau de langue",
                            "💻 Compétences numériques",
                            "🏠 Retour au menu principal"
                        };
                    }
                }
                else
                {
                    // Traiter comme un message normal
                    responseText = await _interviewDialogService.ProcessUserMessage(
                        userMessage, 
                        session.ConversationHistory, 
                        session.CurrentStage, 
                        session.UserProfile);
                    
                    // Ajouter le message du bot à l'historique
                    session.AddBotMessage(responseText);
                    
                    // Options par défaut
                    options = new List<string> {
                        "📋 Commencer la préparation",
                        "💬 Simuler un entretien",
                        "❓ Conseils généraux",
                        "🏠 Menu principal"
                    };
                }

                // Créer les actions suggérées
                var suggestedActions = new SuggestedActions
                {
                    Actions = new List<CardAction>()
                };

                foreach (var option in options)
                {
                    suggestedActions.Actions.Add(new CardAction
                    {
                        Title = option,
                        Type = ActionTypes.ImBack,
                        Value = option
                    });
                }

                var response = new WebChatResponse
                {
                    Text = responseText,
                    SuggestedActions = suggestedActions
                };

                // Journaliser la réponse complète
                _loggingService.LogUserInteraction(userId, userMessage, responseText, session.CurrentStage.ToString());

                return Ok(response);
            }
            catch (Exception ex)
            {
                _loggingService.LogError("webchat", "Erreur lors du traitement du message", ex);
                
                var errorResponse = new WebChatResponse
                {
                    Text = "Désolé, j'ai rencontré un problème. Pouvez-vous réessayer?",
                    SuggestedActions = new SuggestedActions
                    {
                        Actions = new List<CardAction>
                        {
                            new CardAction
                            {
                                Title = "🏠 Menu principal",
                                Type = ActionTypes.ImBack,
                                Value = "🏠 Menu principal"
                            }
                        }
                    }
                };
                
                return Ok(errorResponse);
            }
        }
    }
} 