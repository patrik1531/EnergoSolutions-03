using EnergoSolutions_03.DTO.Wind;

namespace EnergoSolutions_03.Abstraction;

public interface IWindService
{
    Task<WindResponseDto?> GetWindStatsAsync(WindRequestDto dto);
}