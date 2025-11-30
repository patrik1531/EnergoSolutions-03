namespace EnergoSolutions_03.DTO.OpenMeteo;

public class OpenMeteoArchiveClimateDto
{
    public OpenMeteoHourlyClimateDto Hourly { get; set; } = new();
}