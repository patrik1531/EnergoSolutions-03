using EnergoSolutions_03.DTO.Chat;

namespace EnergoSolutions_03.Abstraction;

public interface IChatService
{
    Task<ChatResponseDto?> AskAsync(ChatRequestDto dto);
}