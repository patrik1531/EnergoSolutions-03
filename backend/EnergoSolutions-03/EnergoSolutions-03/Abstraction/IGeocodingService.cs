using EnergoSolutions_03.DTO;
using EnergoSolutions_03.DTO.Geocode;

namespace EnergoSolutions_03.Abstraction;

public interface IGeocodingService
{
    Task<GeocodeResponseDto?> GeocodeAsync(GeocodeRequestDto request);
}