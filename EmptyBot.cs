// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.22.0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InterviewChatbot.Models;
using InterviewChatbot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace InterviewChatbot
{
    public class EmptyBot : ActivityHandler
    {
        private readonly InterviewDialogService _interviewDialogService;
        private readonly MenuDialogService _menuDialogService;
        private readonly LoggingService _loggingService;
        private readonly ILogger<EmptyBot> _logger;
        
        // Dictionnaire pour stocker les sessions d'entretien par utilisateur
        private static readonly Dictionary<string, InterviewSession> _sessions = new Dictionary<string, InterviewSession>();

        public EmptyBot(InterviewDialogService interviewDialogService, MenuDialogService menuDialogService, LoggingService loggingService, ILogger<EmptyBot> logger)
        {
            _interviewDialogService = interviewDialogService;
            _menuDialogService = menuDialogService;
            _loggingService = loggingService;
            _logger = logger;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var userId = turnContext.Activity.From.Id;
            var userMessage = turnContext.Activity.Text;

            // Récupérer ou créer une session pour cet utilisateur
            var session = GetOrCreateSession(userId);

            try
            {
                // Journaliser le message reçu
                _logger.LogInformation($"Message reçu de l'utilisateur {userId}: {userMessage}");
                
                // Vérifier si c'est une commande liée aux boutons ou au menu
                if (await HandleButtonCommands(userMessage, session, turnContext, cancellationToken))
                {
                    return;
                }
                
                // Si ce n'est pas une commande spéciale, traiter comme un message normal
                var response = await _interviewDialogService.ProcessUserMessage(
                    userMessage, 
                    session.ConversationHistory, 
                    session.CurrentStage, 
                    session.UserProfile);

                // Mettre à jour la session
                session.LastUpdatedAt = DateTime.Now;
                
                // Journaliser l'interaction
                _loggingService.LogUserInteraction(
                    userId, 
                    userMessage, 
                    response, 
                    session.CurrentStage.ToString());
                
                // Vérifier si l'utilisateur est prêt à passer à l'étape suivante
                if (session.ConversationHistory.Count >= 4)  // Après quelques échanges
                {
                    bool shouldAdvance = await _interviewDialogService.ShouldAdvanceToNextStage(
                        session.ConversationHistory,
                        session.CurrentStage,
                        session.UserProfile);
                        
                    if (shouldAdvance)
                    {
                        // Sauvegarder l'étape actuelle pour le log
                        var previousStage = session.CurrentStage;
                        
                        // Passer à l'étape suivante
                        AdvanceToNextStage(session);
                        
                        // Journaliser la transition
                        _loggingService.LogStageTransition(
                            userId, 
                            previousStage.ToString(), 
                            session.CurrentStage.ToString());
                        
                        // Ajouter un message indiquant le changement d'étape
                        string stageTransitionMessage = GetStageTransitionMessage(session.CurrentStage);
                        await turnContext.SendActivityAsync(MessageFactory.Text(stageTransitionMessage), cancellationToken);
                        
                        // Afficher les options correspondant à la nouvelle étape
                        await SendStageOptions(turnContext, session.CurrentStage, cancellationToken);
                        return;
                    }
                }
                
                // Envoyer la réponse
                await turnContext.SendActivityAsync(MessageFactory.Text(response), cancellationToken);
                
                // Afficher les options correspondant à l'étape actuelle
                await SendStageOptions(turnContext, session.CurrentStage, cancellationToken);
            }
            catch (Exception ex)
            {
                _loggingService.LogError(userId, "Erreur lors du traitement du message", ex);
                await turnContext.SendActivityAsync(MessageFactory.Text("Désolé, j'ai rencontré un problème. Pouvez-vous réessayer?"), cancellationToken);
                
                // Afficher le menu principal en cas d'erreur
                await turnContext.SendActivityAsync(InteractiveElements.CreateMainMenu(), cancellationToken);
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var userId = member.Id;
                    
                    // Créer une nouvelle session pour ce nouvel utilisateur
                    var session = GetOrCreateSession(userId);
                    
                    // Envoyer un message de bienvenue
                    string welcomeMessage = "Bonjour ! Je suis votre assistant virtuel pour vous aider à préparer vos entretiens d'embauche. Comment puis-je vous aider aujourd'hui ?";
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeMessage), cancellationToken);
                    
                    // Ajouter le message à l'historique
                    session.ConversationHistory.Add(new MessageDto { Role = "assistant", Content = welcomeMessage });
                    
                    // Afficher le menu principal
                    await turnContext.SendActivityAsync(InteractiveElements.CreateMainMenu(), cancellationToken);
                    
                    // Journaliser la nouvelle session
                    _logger.LogInformation($"Nouvelle session créée pour l'utilisateur {userId}");
                }
            }
        }
        
        // Gère les commandes liées aux boutons et menus
        private async Task<bool> HandleButtonCommands(string userMessage, InterviewSession session, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Si le message contient "Menu principal" ou l'emoji maison
            if (userMessage.Contains("Menu principal") || userMessage == "🏠")
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Voici le menu principal :"), cancellationToken);
                await turnContext.SendActivityAsync(InteractiveElements.CreateMainMenu(), cancellationToken);
                return true;
            }
            
            // Menu principal - options
            if (userMessage.StartsWith("📋") || 
                userMessage.StartsWith("💬") || 
                userMessage.StartsWith("📊") || 
                userMessage.StartsWith("❓") || 
                userMessage.StartsWith("⚙️"))
            {
                var (responseMessage, optionsActivity) = await _menuDialogService.ProcessMainMenuSelection(userMessage, session);
                await turnContext.SendActivityAsync(MessageFactory.Text(responseMessage), cancellationToken);
                await turnContext.SendActivityAsync(optionsActivity, cancellationToken);
                return true;
            }
            
            // Menu de préparation - options
            if (session.CurrentStage == InterviewStage.Preparation &&
                (userMessage.StartsWith("🔍") || 
                userMessage.StartsWith("🗣️") || 
                userMessage.StartsWith("👔") || 
                userMessage.StartsWith("📝") || 
                userMessage.Contains("Retour préparation")))
            {
                var (responseMessage, optionsActivity) = await _menuDialogService.ProcessPreparationMenuSelection(userMessage, session);
                await turnContext.SendActivityAsync(MessageFactory.Text(responseMessage), cancellationToken);
                await turnContext.SendActivityAsync(optionsActivity, cancellationToken);
                return true;
            }
            
            // Menu de simulation - options
            if (session.CurrentStage == InterviewStage.Simulation &&
                (userMessage.StartsWith("🚀") || 
                userMessage.StartsWith("⏱️") || 
                userMessage.StartsWith("⏳") || 
                userMessage.StartsWith("🎮")))
            {
                var (responseMessage, optionsActivity) = await _menuDialogService.ProcessSimulationMenuSelection(userMessage, session);
                await turnContext.SendActivityAsync(MessageFactory.Text(responseMessage), cancellationToken);
                await turnContext.SendActivityAsync(optionsActivity, cancellationToken);
                return true;
            }
            
            // Options de réponse de simulation
            if (userMessage.StartsWith("✅") || // Continuer
                userMessage.StartsWith("⏸️") || // Pause
                userMessage.StartsWith("❓") || // Demander conseil
                userMessage.StartsWith("🔄"))   // Refaire la question
            {
                await HandleSimulationResponse(userMessage, session, turnContext, cancellationToken);
                return true;
            }
            
            return false;
        }
        
        // Gère les réponses pendant la simulation
        private async Task HandleSimulationResponse(string userMessage, InterviewSession session, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            switch (userMessage)
            {
                case "✅ Continuer":
                    // Générer la prochaine question
                    string prompt = "Tu es un recruteur qui pose une question d'entretien d'embauche pertinente, " +
                                   $"adaptée au secteur {session.UserProfile.JobSector} et au niveau d'expérience {session.UserProfile.Experience}. " +
                                    "Pose uniquement la question, sans explication supplémentaire.";
                    
                    var messages = new List<MessageDto>();
                    // Prendre les 5 derniers messages pour le contexte
                    if (session.ConversationHistory.Count > 5)
                    {
                        messages.AddRange(session.ConversationHistory.GetRange(
                            session.ConversationHistory.Count - 5, 5));
                    }
                    else
                    {
                        messages.AddRange(session.ConversationHistory);
                    }
                    
                    messages.Add(new MessageDto 
                    { 
                        Role = "user", 
                        Content = "Posez-moi une autre question d'entretien" 
                    });
                    
                    string nextQuestion = await _interviewDialogService.GetInterviewResponse(messages, prompt);
                    
                    // Ajouter la question à l'historique
                    session.ConversationHistory.Add(new MessageDto { Role = "assistant", Content = nextQuestion });
                    
                    await turnContext.SendActivityAsync(MessageFactory.Text(nextQuestion), cancellationToken);
                    await turnContext.SendActivityAsync(InteractiveElements.CreateSimulationResponseOptions(), cancellationToken);
                    break;
                    
                case "⏸️ Pause":
                    await turnContext.SendActivityAsync(MessageFactory.Text("Simulation en pause. Prenez votre temps et cliquez sur 'Continuer' quand vous serez prêt à reprendre."), cancellationToken);
                    await turnContext.SendActivityAsync(InteractiveElements.CreateSimulationResponseOptions(), cancellationToken);
                    break;
                    
                case "❓ Demander un conseil":
                    // Générer un conseil basé sur la dernière question
                    string lastQuestion = GetLastQuestion(session);
                    string advicePrompt = $"Tu es un coach d'entretien qui donne des conseils pour répondre à cette question: '{lastQuestion}'. " +
                                         "Sois concis, concret et donne un exemple de bonne réponse.";
                    
                    var adviceMessages = new List<MessageDto>
                    {
                        new MessageDto { Role = "user", Content = $"Comment répondre à cette question d'entretien: {lastQuestion} ?" }
                    };
                    
                    string advice = await _interviewDialogService.GetInterviewResponse(adviceMessages, advicePrompt);
                    await turnContext.SendActivityAsync(MessageFactory.Text(advice), cancellationToken);
                    await turnContext.SendActivityAsync(InteractiveElements.CreateSimulationResponseOptions(), cancellationToken);
                    break;
                    
                case "🔄 Refaire cette question":
                    // Répéter la dernière question
                    string questionToRepeat = GetLastQuestion(session);
                    await turnContext.SendActivityAsync(MessageFactory.Text("Voici à nouveau la question:"), cancellationToken);
                    await turnContext.SendActivityAsync(MessageFactory.Text(questionToRepeat), cancellationToken);
                    await turnContext.SendActivityAsync(InteractiveElements.CreateSimulationResponseOptions(), cancellationToken);
                    break;
            }
        }
        
        // Obtient la dernière question posée dans la session
        private string GetLastQuestion(InterviewSession session)
        {
            // Rechercher le dernier message "assistant" qui n'est pas une instruction
            for (int i = session.ConversationHistory.Count - 1; i >= 0; i--)
            {
                var message = session.ConversationHistory[i];
                if (message.Role == "assistant" && 
                    !message.Content.Contains("Comment souhaitez-vous continuer") &&
                    !message.Content.Contains("Menu"))
                {
                    return message.Content;
                }
            }
            
            return "Pourriez-vous me parler de votre expérience professionnelle ?";
        }
        
        // Envoie les options correspondant à l'étape actuelle
        private async Task SendStageOptions(ITurnContext<IMessageActivity> turnContext, InterviewStage stage, CancellationToken cancellationToken)
        {
            switch (stage)
            {
                case InterviewStage.Introduction:
                    await turnContext.SendActivityAsync(InteractiveElements.CreateMainMenu(), cancellationToken);
                    break;
                case InterviewStage.Preparation:
                    await turnContext.SendActivityAsync(InteractiveElements.CreatePreparationMenu(), cancellationToken);
                    break;
                case InterviewStage.Simulation:
                    await turnContext.SendActivityAsync(InteractiveElements.CreateSimulationMenu(), cancellationToken);
                    break;
                case InterviewStage.Feedback:
                    await turnContext.SendActivityAsync(InteractiveElements.CreateSimulationResponseOptions(), cancellationToken);
                    break;
                default:
                    await turnContext.SendActivityAsync(InteractiveElements.CreateMainMenu(), cancellationToken);
                    break;
            }
        }
        
        // Récupère ou crée une session pour un utilisateur
        private InterviewSession GetOrCreateSession(string userId)
        {
            if (_sessions.ContainsKey(userId))
            {
                return _sessions[userId];
            }
            
            // Créer un nouveau profil utilisateur
            var profile = new UserProfile 
            { 
                UserId = userId,
                Name = "Utilisateur" // Par défaut
            };
            
            // Créer une nouvelle session
            var session = new InterviewSession
            {
                UserProfile = profile,
                CurrentStage = InterviewStage.Introduction
            };
            
            _sessions[userId] = session;
            _logger.LogInformation($"Nouvelle session créée pour l'utilisateur {userId}");
            
            return session;
        }
        
        // Avance à l'étape suivante de l'entretien
        private void AdvanceToNextStage(InterviewSession session)
        {
            switch (session.CurrentStage)
            {
                case InterviewStage.Introduction:
                    session.CurrentStage = InterviewStage.Preparation;
                    break;
                case InterviewStage.Preparation:
                    session.CurrentStage = InterviewStage.Simulation;
                    break;
                case InterviewStage.Simulation:
                    session.CurrentStage = InterviewStage.Feedback;
                    break;
                case InterviewStage.Feedback:
                    // Revenir à la préparation après le feedback
                    session.CurrentStage = InterviewStage.Preparation;
                    break;
            }
        }
        
        // Obtient un message de transition entre les étapes
        private string GetStageTransitionMessage(InterviewStage stage)
        {
            switch (stage)
            {
                case InterviewStage.Preparation:
                    return "Très bien ! Passons maintenant à l'étape de préparation pour votre entretien.";
                case InterviewStage.Simulation:
                    return "Vous êtes prêt pour simuler un entretien ! Je vais maintenant jouer le rôle d'un recruteur et vous poser des questions.";
                case InterviewStage.Feedback:
                    return "Félicitations pour cette simulation ! Passons maintenant au feedback sur votre performance.";
                default:
                    return "Passons à l'étape suivante.";
            }
        }
    }
}
