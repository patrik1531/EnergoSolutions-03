using EnergoSolutions_03.DTO;
using EnergoSolutions_03.DTO.Climate;

namespace EnergoSolutions_03.Abstraction;

public interface IClimateService
{
    Task<ClimateResponseDto?> GetClimateHeatingAsync(ClimateRequestDto dto);
}