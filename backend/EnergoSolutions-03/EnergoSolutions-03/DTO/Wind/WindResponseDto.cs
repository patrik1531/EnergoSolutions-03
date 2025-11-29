namespace EnergoSolutions_03.DTO.Wind;

public class WindResponseDto
{
    public WindLocationDto Location { get; set; } = new();
    public WindPeriodDto Period { get; set; } = new();
    public List<WindYearDto> Years { get; set; } = new();
    public WindMultiYearDto MultiYear { get; set; } = new();
}