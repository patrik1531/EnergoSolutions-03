namespace EnergoSolutions_03.Models.Agent;

public class CalculationResults
{
    public SystemCalculation SolarSystem { get; set; }
    public SystemCalculation WindSystem { get; set; }
    public SystemCalculation HeatPumpSystem { get; set; }
    public SystemCalculation CombinedSystem { get; set; }
}