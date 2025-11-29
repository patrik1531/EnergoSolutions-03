namespace EnergoSolutions_03.DTO.Climate;

public class ClimateYearDto
{
    public int Year { get; set; }
    public List<TempBinDto> Temp_Bins { get; set; } = new();
    public double Hdd_20 { get; set; }
    public double Min_Temp { get; set; }
    public int Hours_Below_Minus10 { get; set; }
    public int Hours_Below_Minus15 { get; set; }
    public int Total_Hours { get; set; }
}