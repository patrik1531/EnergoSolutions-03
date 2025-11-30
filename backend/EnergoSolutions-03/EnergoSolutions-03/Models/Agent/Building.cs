namespace EnergoSolutions_03.Models.Agent;

public class Building
{
    public string BuildingType { get; set; } = string.Empty;
    public int? HeatedAreaM2 { get; set; }
    public string InsulationLevel { get; set; } = string.Empty;
}