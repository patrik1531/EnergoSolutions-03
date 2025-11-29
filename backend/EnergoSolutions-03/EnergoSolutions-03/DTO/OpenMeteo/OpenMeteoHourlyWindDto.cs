namespace EnergoSolutions_03.DTO.OpenMeteo;

public class OpenMeteoHourlyWindDto
{
    public List<string> Time { get; set; } = new();
    public List<double> WindSpeed_10m { get; set; } = new();
}