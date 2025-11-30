namespace EnergoSolutions_03.Models.Agent;

public class SystemCalculation
{
    public string Technology { get; set; }
    public string SystemSize { get; set; }
    public int NumberOfPanels { get; set; }
    public string YearlyProduction { get; set; }
    public double InstallationCost { get; set; }
    public double YearlySavings { get; set; }
    public double PaybackYears { get; set; }
    public double ROI { get; set; }
    public Dictionary<string, object> Details { get; set; }
}