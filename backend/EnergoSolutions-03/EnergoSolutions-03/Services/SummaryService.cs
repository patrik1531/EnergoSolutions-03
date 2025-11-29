using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.DTO.Summary;
using EnergoSolutions_03.DTO.Climate;
using EnergoSolutions_03.DTO.Wind;
using EnergoSolutions_03.DTO.Solar;

namespace EnergoSolutions_03.Services;

public class SummaryService : ISummaryService
{
    private readonly IClimateService _climate;
    private readonly IWindService _wind;
    private readonly ISolarService _solar;

    public SummaryService(
        IClimateService climate,
        IWindService wind,
        ISolarService solar)
    {
        _climate = climate;
        _wind = wind;
        _solar = solar;
    }

    public async Task<SummaryResponseDto> BuildSummaryAsync(SummaryRequestDto dto)
    {
        var output = new SummaryResponseDto
        {
            Location = new SummaryLocationDto
            {
                Lat = dto.Lat,
                Lon = dto.Lon
            }
        };

        var warnings = new List<SummaryWarningDto>();

        // HEATING
        try
        {
            ClimateResponseDto? heating = await _climate.GetClimateHeatingAsync(
                new ClimateRequestDto { Lat = dto.Lat, Lon = dto.Lon });

            if (heating != null)
                output.ClimateHeating = new SummaryHeatingDto { Data = heating };
        }
        catch (Exception ex)
        {
            warnings.Add(new SummaryWarningDto
            {
                Source = "heating",
                Message = ex.Message
            });
        }

        // WIND
        try
        {
            WindResponseDto? wind = await _wind.GetWindStatsAsync(
                new WindRequestDto { Lat = dto.Lat, Lon = dto.Lon });

            if (wind != null)
                output.ClimateWind = new SummaryWindDto { Data = wind };
        }
        catch (Exception ex)
        {
            warnings.Add(new SummaryWarningDto
            {
                Source = "wind",
                Message = ex.Message
            });
        }

        // SOLAR
        try
        {
            SolarResponseDto? solar = await _solar.GetSolarResourceAsync(
                new SolarRequestDto { Lat = dto.Lat, Lon = dto.Lon });

            if (solar != null)
                output.SolarResource = new SummarySolarDto { Data = solar };
        }
        catch (Exception ex)
        {
            warnings.Add(new SummaryWarningDto
            {
                Source = "solar",
                Message = ex.Message
            });
        }

        if (warnings.Count > 0)
            output.Warnings = warnings;

        return output;
    }
}