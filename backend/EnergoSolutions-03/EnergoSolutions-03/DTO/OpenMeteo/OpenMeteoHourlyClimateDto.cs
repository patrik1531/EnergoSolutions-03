namespace EnergoSolutions_03.DTO.OpenMeteo;

public class OpenMeteoHourlyClimateDto
{
    public List<string> Time { get; set; } = new();
    public List<double> Temperature_2m { get; set; } = new();
}