using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.DTO.Pvgis;
using EnergoSolutions_03.DTO.Solar;
using System.Globalization;

namespace EnergoSolutions_03.Services;

public class SolarService : ISolarService
{
    private readonly HttpClient _http;

    public SolarService(HttpClient http)
    {
        _http = http;
    }

    public async Task<SolarResponseDto?> GetSolarResourceAsync(SolarRequestDto dto)
    {
        double peakPowerKw = 1.0;
        double systemLossPercent = dto.SystemLossPercent ?? 14.0;

        var baseParams = new Dictionary<string, string>
        {
            ["lat"] = dto.Lat.ToString(CultureInfo.InvariantCulture),
            ["lon"] = dto.Lon.ToString(CultureInfo.InvariantCulture),
            ["peakpower"] = peakPowerKw.ToString(CultureInfo.InvariantCulture),
            ["loss"] = systemLossPercent.ToString(CultureInfo.InvariantCulture),
            ["outputformat"] = "json",
            ["mountingplace"] = "building"
        };

        double optimalTilt = await FetchOptimalTiltAsync(baseParams);
        if (double.IsNaN(optimalTilt))
            optimalTilt = 35.0;

        var orientations = new List<(string Name, double AspectDeg)>
        {
            ("south", 0.0),
            ("east", -90.0),
            ("west", 90.0),
            ("north", 180.0)
        };

        var results = new List<SolarOrientationDto>();
        var warnings = new List<string>();

        foreach (var (name, aspect) in orientations)
        {
            var parameters = new Dictionary<string, string>(baseParams)
            {
                ["optimalangles"] = "0",
                ["angle"] = optimalTilt.ToString(CultureInfo.InvariantCulture),
                ["aspect"] = aspect.ToString(CultureInfo.InvariantCulture)
            };

            string url = BuildQuery("PVcalc", parameters);

            PvgisResponseDto? resp;
            try
            {
                resp = await _http.GetFromJsonAsync<PvgisResponseDto>(url);
            }
            catch (Exception ex)
            {
                warnings.Add($"{name}: {ex.Message}");
                continue;
            }

            double yearlyKwh = 0.0;

            double? ey = resp?.Outputs?.Totals?.Fixed?.E_y;
            if (ey.HasValue)
                yearlyKwh = ey.Value;

            results.Add(new SolarOrientationDto
            {
                Orientation = name,
                AspectDeg = aspect,
                KwhPerKwpYear = yearlyKwh
            });
        }

        if (results.Count == 0)
            return null;

        double? southValue = results
            .FirstOrDefault(r => r.Orientation == "south")
            ?.KwhPerKwpYear;

        string bestOrientation = results
            .OrderByDescending(r => r.KwhPerKwpYear)
            .First().Orientation;

        foreach (var r in results)
        {
            if (southValue.HasValue && southValue.Value > 0)
                r.RelativeToSouth = r.KwhPerKwpYear / southValue.Value;
            else
                r.RelativeToSouth = null;
        }

        var response = new SolarResponseDto
        {
            Location = new SolarLocationDto
            {
                Lat = dto.Lat,
                Lon = dto.Lon
            },
            SystemConfig = new SolarSystemConfigDto
            {
                PeakPowerKw = peakPowerKw,
                SystemLossPercent = systemLossPercent,
                OptimalTiltDeg = optimalTilt
            },
            SolarResource = new SolarResourceDto
            {
                Orientations = results,
                BestOrientation = bestOrientation
            }
        };

        if (warnings.Count > 0)
            response.Warnings = warnings;

        return response;
    }

    private async Task<double> FetchOptimalTiltAsync(Dictionary<string, string> baseParams)
    {
        var parameters = new Dictionary<string, string>(baseParams)
        {
            ["optimalangles"] = "1"
        };

        string url = BuildQuery("PVcalc", parameters);

        PvgisResponseDto? resp;
        try
        {
            resp = await _http.GetFromJsonAsync<PvgisResponseDto>(url);
        }
        catch
        {
            return double.NaN;
        }

        double? angle = resp?
            .Outputs?
            .Inputs?
            .Mounting_system?
            .Fixed?
            .Angle;

        return angle ?? double.NaN;
    }

    private static string BuildQuery(string path, Dictionary<string, string> query)
    {
        var parts = new List<string>();
        foreach (var kv in query)
        {
            string encodedKey = Uri.EscapeDataString(kv.Key);
            string encodedValue = Uri.EscapeDataString(kv.Value);
            parts.Add($"{encodedKey}={encodedValue}");
        }

        string queryString = string.Join("&", parts);
        return $"{path}?{queryString}";
    }
}