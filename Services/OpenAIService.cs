using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using InterviewChatbot.Models;

namespace InterviewChatbot.Services
{
    public class OpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _endpoint = "https://api.openai.com/v1/chat/completions";
        private readonly string _model = "gpt-4o";

        public OpenAIService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["OpenAI:ApiKey"];
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new ArgumentNullException("La clé API OpenAI n'est pas configurée.");
            }

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> GetInterviewResponse(List<MessageDto> conversationHistory, string systemPrompt = null)
        {
            var messages = new List<MessageDto>();
            
            // Ajouter le prompt système si fourni
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messages.Add(new MessageDto { Role = "system", Content = systemPrompt });
            }
            
            // Ajouter l'historique de la conversation
            messages.AddRange(conversationHistory);

            var requestData = new
            {
                model = _model,
                messages = messages,
                temperature = 0.7,
                max_tokens = 1000
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(_endpoint, content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<OpenAIResponse>(responseBody);

                return responseObject?.Choices?[0]?.Message?.Content;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la communication avec OpenAI: {ex.Message}");
                return "Désolé, je rencontre des difficultés techniques pour répondre à votre question. Veuillez réessayer plus tard.";
            }
        }
    }

    public class OpenAIResponse
    {
        [JsonProperty("choices")]
        public List<Choice> Choices { get; set; }
    }

    public class Choice
    {
        [JsonProperty("message")]
        public MessageDto Message { get; set; }
    }
} 