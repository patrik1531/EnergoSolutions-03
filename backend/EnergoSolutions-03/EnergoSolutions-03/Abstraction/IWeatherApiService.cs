using EnergoSolutions_03.Models.Agent;

namespace EnergoSolutions_03.Abstraction;

public interface IWeatherApiService
{
    Task<(double Lat, double Lon)> GetCoordinates(string address);
    Task<TechnicalData> GetSummaryData(double lat, double lon);
}