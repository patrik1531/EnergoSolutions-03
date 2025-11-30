namespace EnergoSolutions_03.DTO.Summary;

public class SummaryResponseDto
{
    public SummaryLocationDto Location { get; set; } = new();
    public SummaryHeatingDto? ClimateHeating { get; set; }
    public SummaryWindDto? ClimateWind { get; set; }
    public SummarySolarDto? SolarResource { get; set; }
    public List<SummaryWarningDto>? Warnings { get; set; }
}