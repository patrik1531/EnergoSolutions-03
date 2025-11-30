using System.Globalization;
using System.Net.Http.Json;
using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.DTO;
using EnergoSolutions_03.DTO.Geocode;

namespace EnergoSolutions_03.Services;

internal sealed class NominatimResult
{
    public string? display_name { get; set; }
    public string? lat { get; set; }
    public string? lon { get; set; }
}

public class GeocodingService : IGeocodingService
{
    private readonly HttpClient _http;

    public GeocodingService(HttpClient http)
    {
        _http = http;
    }

    public async Task<GeocodeResponseDto?> GeocodeAsync(string address)
    {
        var url = $"search?q={Uri.EscapeDataString(address)}&format=json&limit=1";

        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        var results = await response.Content.ReadFromJsonAsync<List<NominatimResult>>();
        var first = results?.FirstOrDefault();

        if (first is null || first.lat is null || first.lon is null)
            return null;

        return new GeocodeResponseDto
        {
            Address = first.display_name ?? address,
            Latitude = double.Parse(first.lat, CultureInfo.InvariantCulture),
            Longitude = double.Parse(first.lon, CultureInfo.InvariantCulture)
        };
    }
}