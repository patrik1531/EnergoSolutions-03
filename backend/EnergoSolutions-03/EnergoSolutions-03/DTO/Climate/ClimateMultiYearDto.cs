namespace EnergoSolutions_03.DTO.Climate;

public class ClimateMultiYearDto
{
    public List<MultiYearTempBinDto> TempBinsAvgPercent { get; set; } = new();
    public int TotalYears { get; set; }
}