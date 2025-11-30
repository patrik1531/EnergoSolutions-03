using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.DTO.Chat;


namespace EnergoSolutions_03.Services;

public class ChatService : IChatService
{
    private readonly HttpClient _http;

    public ChatService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ChatResponseDto?> AskAsync(ChatRequestDto dto)
    {
        var request = new OpenAIRequestDto
        {
            Model = "gpt-4.1",
            Messages = new List<OpenAIMessageDto>
            {
                new OpenAIMessageDto
                {
                    Role = "user",
                    Content = dto.Prompt
                }
            }
        };

        var response = await _http.PostAsJsonAsync("chat/completions", request);
        if (!response.IsSuccessStatusCode)
            return null;

        var data = await response.Content.ReadFromJsonAsync<OpenAIResponseDto>();
        if (data == null || data.Choices.Count == 0)
            return null;

        return new ChatResponseDto
        {
            Response = data.Choices[0].Message.Content
        };
    }
}