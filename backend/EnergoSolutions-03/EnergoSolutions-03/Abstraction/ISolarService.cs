using EnergoSolutions_03.DTO.Solar;

namespace EnergoSolutions_03.Abstraction;

public interface ISolarService
{
    Task<SolarResponseDto?> GetSolarResourceAsync(SolarRequestDto dto);
}