using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnergoSolutions_03.Abstraction;

namespace EnergoSolutions_03.Services;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;

    public OpenAIService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetCompletion(string prompt)
    {
        // OpenAI request body
        var requestBody = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "system", content = "You are a strict JSON extraction assistant. Respond ONLY with valid JSON." },
                new { role = "user", content = prompt }
            },
            temperature = 0
        };

        // Serialize body
        var jsonRequest = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.PostAsync("chat/completions", content);
        }
        catch (Exception ex)
        {
            return $"AI network error: {ex.Message}";
        }

        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return $"AI API error: {response.StatusCode} → {responseJson}";
        }

        // Parse OpenAI response
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0)
            {
                var first = choices[0];

                // Standard response
                if (first.TryGetProperty("message", out var msg) &&
                    msg.TryGetProperty("content", out var cont))
                {
                    return cont.GetString() ?? "";
                }

                // Streaming delta fallback
                if (first.TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("content", out var deltaContent))
                {
                    return deltaContent.GetString() ?? "";
                }
            }

            return "AI error: no 'content' field found in response.";
        }
        catch (Exception ex)
        {
            return $"AI parsing error: {ex.Message}";
        }
    }
}