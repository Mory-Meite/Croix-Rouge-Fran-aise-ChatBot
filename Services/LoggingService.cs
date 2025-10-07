using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text;

namespace InterviewChatbot.Services
{
    public class LoggingService
    {
        private readonly ILogger<LoggingService> _logger;
        private readonly string _logFilePath;
        // Objet de verrouillage pour synchroniser l'accès au fichier
        private static readonly object _fileLock = new object();

        public LoggingService(ILogger<LoggingService> logger)
        {
            _logger = logger;
            
            // Créer un dossier Logs s'il n'existe pas
            var logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }
            
            // Créer un fichier de log par jour
            string dateString = DateTime.Now.ToString("yyyy-MM-dd");
            _logFilePath = Path.Combine(logsDirectory, $"interview_chatbot_{dateString}.log");
        }

        public void LogUserInteraction(string userId, string userMessage, string botResponse, string stage)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = $"[{timestamp}] [Utilisateur: {userId}] [Étape: {stage}]\n" +
                              $"Message utilisateur: {userMessage}\n" +
                              $"Réponse bot: {botResponse}\n" +
                              $"---------------------------------------------\n";
            
            // Log dans le fichier de manière synchronisée
            WriteToLogFile(logEntry);
            
            // Log dans le système de logs standard
            _logger.LogInformation($"Interaction: User {userId} - Stage {stage}");
        }
        
        public void LogError(string userId, string errorMessage, Exception ex = null)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = $"[{timestamp}] [Utilisateur: {userId}] [ERREUR]\n" +
                              $"Message d'erreur: {errorMessage}\n";
                              
            if (ex != null)
            {
                logEntry += $"Exception: {ex.Message}\n" +
                            $"StackTrace: {ex.StackTrace}\n";
            }
            
            logEntry += "---------------------------------------------\n";
            
            // Log dans le fichier de manière synchronisée
            WriteToLogFile(logEntry);
            
            // Log dans le système de logs standard
            _logger.LogError($"Error for User {userId}: {errorMessage}");
        }
        
        public void LogStageTransition(string userId, string fromStage, string toStage)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = $"[{timestamp}] [Utilisateur: {userId}] [TRANSITION]\n" +
                              $"De l'étape: {fromStage}\n" +
                              $"Vers l'étape: {toStage}\n" +
                              $"---------------------------------------------\n";
            
            // Log dans le fichier de manière synchronisée
            WriteToLogFile(logEntry);
            
            // Log dans le système de logs standard
            _logger.LogInformation($"Stage transition for User {userId}: {fromStage} -> {toStage}");
        }

        // Méthode pour écrire dans le fichier de log avec verrouillage
        private void WriteToLogFile(string logEntry)
        {
            try
            {
                // Utiliser un bloc lock pour éviter les accès concurrents
                lock (_fileLock)
                {
                    // Utiliser FileShare.ReadWrite pour permettre à d'autres processus de lire le fichier
                    using (var fs = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using (var sw = new StreamWriter(fs, Encoding.UTF8))
                    {
                        sw.Write(logEntry);
                        sw.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                // Si on ne peut pas écrire dans le fichier, au moins loguer dans les logs système
                _logger.LogError($"Erreur lors de l'écriture dans le fichier de log: {ex.Message}");
            }
        }
    }
} 