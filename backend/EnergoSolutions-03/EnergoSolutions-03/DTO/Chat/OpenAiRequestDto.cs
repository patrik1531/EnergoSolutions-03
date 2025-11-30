namespace EnergoSolutions_03.DTO.Chat;

public class OpenAIRequestDto
{
    public string Model { get; set; } = "gpt-4.1";
    public List<OpenAIMessageDto> Messages { get; set; } = new();
}