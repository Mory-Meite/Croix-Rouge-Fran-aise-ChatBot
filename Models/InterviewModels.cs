using System;
using System.Collections.Generic;

namespace InterviewChatbot.Models
{
    /// <summary>
    /// Représente les différentes étapes du processus d'entretien
    /// </summary>
    public enum InterviewStage
    {
        Introduction,    // Présentation de l'outil et évaluation des besoins
        Preparation,     // Conseils et préparation à l'entretien
        Simulation,      // Simulation de l'entretien avec questions-réponses
        Feedback         // Analyse et conseils post-simulation
    }

    /// <summary>
    /// Représente le profil d'un utilisateur avec ses caractéristiques
    /// </summary>
    public class UserProfile
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string PreferredLanguage { get; set; } = "French";
        public string JobSector { get; set; } = "Non spécifié";
        public string Experience { get; set; } = "Débutant";
        
        // Niveau de langue (Débutant, Intermédiaire, Avancé)
        public string LanguageLevel { get; set; } = "Intermédiaire";
        
        // Niveau d'aisance avec les outils numériques (Faible, Moyen, Élevé)
        public string DigitalSkillLevel { get; set; } = "Faible";
        
        // Les réponses sauvegardées de l'utilisateur pour référence future
        public Dictionary<string, string> SavedResponses { get; set; } = new Dictionary<string, string>();
        
        // État actuel de l'interaction avec l'utilisateur
        public string CurrentState { get; set; } = "introduction";
        
        // Étape actuelle dans le processus d'évaluation
        public int EvaluationStep { get; set; } = 0;
        
        // Profil de vulnérabilité basé sur les réponses de l'utilisateur
        public Dictionary<string, string> VulnerabilityProfile { get; set; } = new Dictionary<string, string>();
        
        // Niveau d'expérience en entretien (débutant, intermédiaire, avancé)
        public string ExperienceLevel { get; set; } = "débutant";
        
        // Niveau d'anxiété (faible, moyen, élevé)
        public string AnxietyLevel { get; set; } = "moyen";
        
        // Format d'apprentissage préféré (progressif, réaliste, mixte)
        public string LearningPreference { get; set; } = "progressif";
        
        // Besoins d'adaptation spécifiques
        public string SpecificNeeds { get; set; } = "";
        
        // Indique si le profil a été évalué
        public bool IsProfileEvaluated { get; set; } = false;
    }

    /// <summary>
    /// Représente une session d'entretien avec l'historique des conversations
    /// </summary>
    public class InterviewSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public UserProfile UserProfile { get; set; }
        public InterviewStage CurrentStage { get; set; } = InterviewStage.Introduction;
        public List<MessageDto> ConversationHistory { get; set; } = new List<MessageDto>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastUpdatedAt { get; set; } = DateTime.Now;
        
        // Stocke les évaluations et feedbacks pour cette session
        public Dictionary<string, string> Feedback { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Ajoute un message de l'utilisateur à l'historique des conversations
        /// </summary>
        public void AddUserMessage(string message)
        {
            ConversationHistory.Add(new MessageDto { Role = "user", Content = message });
            LastUpdatedAt = DateTime.Now;
        }
        
        /// <summary>
        /// Ajoute un message du bot à l'historique des conversations
        /// </summary>
        public void AddBotMessage(string message)
        {
            ConversationHistory.Add(new MessageDto { Role = "assistant", Content = message });
            LastUpdatedAt = DateTime.Now;
        }
        
        /// <summary>
        /// Récupère ou crée une session pour un utilisateur donné
        /// </summary>
        public static InterviewSession GetOrCreateSession(string userId)
        {
            // Dictionnaire pour stocker les sessions d'entretien par utilisateur
            // Note: normalement, cela devrait être dans une base de données ou un service dédié
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
            return session;
        }
        
        // Dictionnaire statique pour stocker les sessions d'entretien par utilisateur
        private static readonly Dictionary<string, InterviewSession> _sessions = new Dictionary<string, InterviewSession>();
    }

    /// <summary>
    /// Représente une question d'entretien avec des conseils pour la réponse
    /// </summary>
    public class InterviewQuestion
    {
        public string Id { get; set; }
        public string QuestionText { get; set; }
        public string Category { get; set; }  // Ex: "Expérience", "Compétences", "Motivation"
        public string Difficulty { get; set; }  // "Facile", "Moyen", "Difficile"
        public string TipsForAnswer { get; set; }
        public List<string> JobSectors { get; set; } = new List<string>();  // Secteurs d'emploi pour lesquels cette question est pertinente
    }
} 