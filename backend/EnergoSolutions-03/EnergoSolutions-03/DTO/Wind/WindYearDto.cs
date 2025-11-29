namespace EnergoSolutions_03.DTO.Wind;

public class WindYearDto
{
    public int Year { get; set; }
    public List<WindBinDto> WindBins { get; set; }
    public double MeanSpeed { get; set; }
    public int HoursAbove3 { get; set; }
    public int HoursAbove6 { get; set; }
    public int TotalHours { get; set; }
}