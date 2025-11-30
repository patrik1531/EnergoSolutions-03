using System.Text.Json;
using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.Models.Agent;

namespace EnergoSolutions_03.Services;

public class WeatherApiService : IWeatherApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IGeocodingService _geocodingService;

        public WeatherApiService(HttpClient httpClient, IGeocodingService geocodingService)
        {
            _httpClient = httpClient;
            _geocodingService = geocodingService;
        }

        public async Task<(double Lat, double Lon)> GetCoordinates(string address)
        {
            // Volaj váš backend API
            try
            {
                var response = await _geocodingService.GeocodeAsync(address);
                return (response.Latitude, response.Longitude);
            }
            catch
            {
                // Fallback pre Košice
                return (48.7164, 21.2611);
            }
        }

        public async Task<TechnicalData> GetSummaryData(double lat, double lon)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("http://localhost:5000/summary",
                    new { lat, lon });
                var summary = await response.Content.ReadAsStringAsync();

                // Parse summary JSON a mapuj na TechnicalData
                return ParseSummaryToTechnicalData(summary);
            }
            catch
            {
                // Fallback data
                return new TechnicalData
                {
                    SolarResource = new SolarResource
                    {
                        YearlyKwhPerKwp = 1000,
                        OptimalAngle = 35
                    },
                    WindData = new WindData
                    {
                        AverageSpeed = 4.5,
                        DaysAbove4ms = 150
                    },
                    ClimateData = new ClimateData
                    {
                        YearAverageTemp = 10,
                        HeatingDays = 200
                    }
                };
            }
        }

        private TechnicalData ParseSummaryToTechnicalData(string summaryJson)
        {
            var doc = JsonDocument.Parse(summaryJson);
            var root = doc.RootElement;

            return new TechnicalData
            {
                SolarResource = new SolarResource
                {
                    YearlyKwhPerKwp = root.GetProperty("solar_resource")
                        .GetProperty("solar_resource")
                        .GetProperty("orientations")[0]
                        .GetProperty("kwh_per_kwp_year")
                        .GetDouble(),
                    OptimalAngle = root.GetProperty("solar_resource")
                        .GetProperty("system_config")
                        .GetProperty("optimal_tilt_deg")
                        .GetDouble()
                },
                WindData = new WindData
                {
                    AverageSpeed = root.GetProperty("climate_wind")
                        .GetProperty("multi_year")
                        .GetProperty("overall_mean_speed")
                        .GetDouble()
                },
                ClimateData = new ClimateData
                {
                    YearAverageTemp = 10, // Približne pre SK
                    HeatingDays = 200
                }
            };
        }
    }
