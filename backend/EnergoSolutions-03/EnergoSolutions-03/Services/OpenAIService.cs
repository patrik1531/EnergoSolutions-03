using System.Text;
using System.Text.Json;
using EnergoSolutions_03.Abstraction;

namespace EnergoSolutions_03.Services;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAIService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenAI:ApiKey"] ?? "sk-your-key";
    }

    public async Task<string> GetCompletion(string prompt)
    {
        try
        {
            var request = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = "Si energetický expert. Odpovedaj stručne a v slovenčine." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
                
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseJson = await response.Content.ReadAsStringAsync();
                
            var result = JsonDocument.Parse(responseJson);
            return result.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
        catch
        {
            // Fallback pre demo bez OpenAI
            return "Test response - OpenAI not configured";
        }
    }
}