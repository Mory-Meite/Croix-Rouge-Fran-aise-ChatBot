using Newtonsoft.Json;

namespace InterviewChatbot.Models
{
    /// <summary>
    /// Représente un message dans une conversation entre l'utilisateur et le chatbot
    /// </summary>
    public class MessageDto
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }
} 