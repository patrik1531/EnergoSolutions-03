namespace EnergoSolutions_03.Models.Agent;

public class AnalysisResults
{
    public TechnologyScore SolarPotential { get; set; } = new();
    public TechnologyScore WindPotential { get; set; } = new();
    public TechnologyScore HeatPumpPotential { get; set; } = new();
    public List<string> RecommendedTechnologies { get; set; } = new();
}