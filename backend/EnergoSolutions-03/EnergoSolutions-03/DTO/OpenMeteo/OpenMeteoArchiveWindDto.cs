namespace EnergoSolutions_03.DTO.OpenMeteo;

public class OpenMeteoArchiveWindDto
{
    public OpenMeteoHourlyWindDto Hourly { get; set; } = new();
}