namespace EnergoSolutions_03.DTO.Climate;

public class ClimateResponseDto
{
    public LocationDto Location { get; set; } = new();
    public PeriodDto Period { get; set; } = new();
    public List<ClimateYearDto> Years { get; set; } = new();
    public ClimateMultiYearDto MultiYear { get; set; } = new();
}