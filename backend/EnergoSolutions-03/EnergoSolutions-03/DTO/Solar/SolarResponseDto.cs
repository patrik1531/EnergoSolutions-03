namespace EnergoSolutions_03.DTO.Solar;

public class SolarResponseDto
{
    public SolarLocationDto Location { get; set; } = new();
    public SolarSystemConfigDto SystemConfig { get; set; } = new();
    public SolarResourceDto SolarResource { get; set; } = new();
    public List<string>? Warnings { get; set; }
}