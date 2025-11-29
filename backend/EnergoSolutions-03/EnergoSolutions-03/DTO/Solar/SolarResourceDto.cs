namespace EnergoSolutions_03.DTO.Solar;

public class SolarResourceDto
{
    public List<SolarOrientationDto> Orientations { get; set; } = new();
    public string BestOrientation { get; set; } = string.Empty;
}