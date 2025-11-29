namespace EnergoSolutions_03.DTO.Climate;

public class ClimateYearDto
{
    public int Year { get; set; }
    public List<TempBinDto> TempBins { get; set; }
    public double Hdd20 { get; set; }
    public double MinTemp { get; set; }
    public int HoursBelowMinus10 { get; set; }
    public int HoursBelowMinus15 { get; set; }
    public int TotalHours { get; set; }
}