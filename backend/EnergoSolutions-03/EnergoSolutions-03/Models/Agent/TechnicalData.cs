namespace EnergoSolutions_03.Models.Agent;

public class TechnicalData
{
    public SolarResource SolarResource { get; set; } = new();
    public WindData WindData { get; set; } = new();
    public ClimateData ClimateData { get; set; } = new();
}