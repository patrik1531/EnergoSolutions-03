using EnergoSolutions_03.DTO.Climate;

namespace EnergoSolutions_03.DTO.Summary;

public class SummaryHeatingDto
{
    public ClimateResponseDto Data { get; set; } = new();
}