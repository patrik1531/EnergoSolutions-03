namespace EnergoSolutions_03.DTO.Chat;

public class OpenAIResponseDto
{
    public List<OpenAIChoiceDto> Choices { get; set; } = new();
}