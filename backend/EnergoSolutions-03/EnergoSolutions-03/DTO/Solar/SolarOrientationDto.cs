namespace EnergoSolutions_03.DTO.Solar;

public class SolarOrientationDto
{
    public string Orientation { get; set; } = string.Empty;
    public double AspectDeg { get; set; }
    public double KwhPerKwpYear { get; set; }
    public double? RelativeToSouth { get; set; }
}