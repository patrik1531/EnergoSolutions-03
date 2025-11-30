namespace EnergoSolutions_03.Models.Agent;

public class CalculationResults
{
    public SystemCalculation SolarSystem { get; set; } = new();
    public SystemCalculation? WindSystem { get; set; } = new();
    public SystemCalculation HeatPumpSystem { get; set; } = new();
    public SystemCalculation CombinedSystem { get; set; } = new();
}