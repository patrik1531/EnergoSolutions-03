namespace EnergoSolutions_03.Models.Agent;

public class AnalysisResults
{
    public TechnologyScore SolarPotential { get; set; }
    public TechnologyScore WindPotential { get; set; }
    public TechnologyScore HeatPumpPotential { get; set; }
    public List<string> RecommendedTechnologies { get; set; }
}