using EnergoSolutions_03.DTO.Summary;

namespace EnergoSolutions_03.Abstraction;

public interface ISummaryService
{
    Task<SummaryResponseDto> BuildSummaryAsync(float lat, float lon);
}