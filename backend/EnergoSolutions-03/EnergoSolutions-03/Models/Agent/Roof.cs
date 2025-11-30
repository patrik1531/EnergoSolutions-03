namespace EnergoSolutions_03.Models.Agent;

public class Roof
{
    public int? RoofAreaM2 { get; set; }
    public List<string> Orientations { get; set; } = new();
}