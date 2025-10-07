using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace InterviewChatbot.Models
{
    /// <summary>
    /// Fournit des méthodes pour créer des éléments interactifs comme des boutons
    /// </summary>
    public static class InteractiveElements
    {
        /// <summary>
        /// Crée un message avec des boutons pour les options
        /// </summary>
        public static Activity CreateButtonMessage(string text, List<string> options)
        {
            var activity = MessageFactory.Text(text);
            activity.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
            };

            foreach (var option in options)
            {
                activity.SuggestedActions.Actions.Add(new CardAction()
                {
                    Title = option,
                    Type = ActionTypes.ImBack,
                    Value = option
                });
            }

            return activity;
        }
        
        /// <summary>
        /// Crée une carte adaptative avec des boutons, des images et du texte formaté
        /// </summary>
        public static Attachment CreateAdaptiveCard(string title, string text, string imageUrl = null, List<CardAction> actions = null)
        {
            var card = new AdaptiveCards.AdaptiveCard(new AdaptiveCards.AdaptiveSchemaVersion(1, 0));
            
            // Ajouter le titre
            card.Body.Add(new AdaptiveCards.AdaptiveTextBlock
            {
                Text = title,
                Size = AdaptiveCards.AdaptiveTextSize.Large,
                Weight = AdaptiveCards.AdaptiveTextWeight.Bolder
            });
            
            // Ajouter l'image si fournie
            if (!string.IsNullOrEmpty(imageUrl))
            {
                card.Body.Add(new AdaptiveCards.AdaptiveImage
                {
                    Url = new System.Uri(imageUrl),
                    Size = AdaptiveCards.AdaptiveImageSize.Medium,
                    Style = AdaptiveCards.AdaptiveImageStyle.Default
                });
            }
            
            // Ajouter le texte
            card.Body.Add(new AdaptiveCards.AdaptiveTextBlock
            {
                Text = text,
                Wrap = true
            });
            
            // Ajouter les boutons d'action si fournis
            if (actions != null)
            {
                foreach (var action in actions)
                {
                    card.Actions.Add(new AdaptiveCards.AdaptiveSubmitAction
                    {
                        Title = action.Title,
                        Data = action.Value
                    });
                }
            }
            
            return new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(card))
            };
        }
        
        /// <summary>
        /// Crée un menu principal avec toutes les options disponibles
        /// </summary>
        public static Activity CreateMainMenu()
        {
            var options = new List<string>
            {
                "👤 Évaluer mon profil",
                "📋 Commencer la préparation",
                "💬 Simuler un entretien",
                "📊 Voir mon feedback",
                "❓ Conseils généraux",
                "⚙️ Mettre à jour mon profil"
            };
            
            return CreateButtonMessage("📱 Menu Principal - Que souhaitez-vous faire ?", options);
        }
        
        /// <summary>
        /// Crée un menu pour la phase de préparation
        /// </summary>
        public static Activity CreatePreparationMenu()
        {
            var options = new List<string>
            {
                "🔍 Rechercher l'entreprise",
                "🗣️ Questions fréquentes",
                "👔 Conseils de présentation",
                "📝 Préparer mes réponses",
                "🏠 Retour au menu principal"
            };
            
            return CreateButtonMessage("🔄 Préparation à l'entretien - Choisissez une option :", options);
        }
        
        /// <summary>
        /// Crée un menu pour la phase de simulation
        /// </summary>
        public static Activity CreateSimulationMenu()
        {
            var options = new List<string>
            {
                "🚀 Démarrer la simulation",
                "⏱️ Simulation courte (5-10 min)",
                "⏳ Simulation complète (15-20 min)",
                "🎮 Simulation par thème",
                "🏠 Retour au menu principal"
            };
            
            return CreateButtonMessage("🎬 Simulation d'entretien - Choisissez une option :", options);
        }
        
        /// <summary>
        /// Crée des options pour les réponses de simulation
        /// </summary>
        public static Activity CreateSimulationResponseOptions()
        {
            var options = new List<string>
            {
                "✅ Continuer",
                "⏸️ Pause",
                "❓ Demander un conseil",
                "🔄 Refaire cette question",
                "🏠 Retour au menu principal"
            };
            
            return CreateButtonMessage("Comment souhaitez-vous continuer ?", options);
        }
    }
} 