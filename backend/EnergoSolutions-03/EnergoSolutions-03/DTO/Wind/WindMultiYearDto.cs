namespace EnergoSolutions_03.DTO.Wind;

public class WindMultiYearDto
{
    public List<MultiYearWindBinDto> WindBinsAvgPercent { get; set; } = new();
    public double? OverallMeanSpeed { get; set; }
    public int TotalYears { get; set; }
}