using System.Text;
using System.Text.Json;

namespace SportsAPP.Services
{
    public class GroqApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GroqApiClient> _logger;
        private const string GroqApiUrl = "https://api.groq.com/openai/v1/chat/completions";

        public GroqApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<GroqApiClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> CompleteAsync(string prompt, int maxTokens = 150)
        {
            var apiKey = _configuration["GroqApi:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_GROQ_API_KEY_HERE")
            {
                _logger.LogWarning("Groq API key not configured. Skipping AI moderation.");
                return "APPROVED";
            }

            var model = _configuration.GetValue<string>("GroqApi:Model", "llama-3.1-8b-instant");

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "You are a content moderation assistant. Be concise and direct." },
                    new { role = "user", content = prompt }
                },
                max_tokens = maxTokens,
                temperature = 0.3
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            try
            {
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(GroqApiUrl, content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseContent);

                var messageContent = jsonResponse.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return messageContent?.Trim() ?? "APPROVED";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Groq API");
                // Graceful degradation: if AI fails, allow content through
                return "APPROVED";
            }
        }
    }
}
