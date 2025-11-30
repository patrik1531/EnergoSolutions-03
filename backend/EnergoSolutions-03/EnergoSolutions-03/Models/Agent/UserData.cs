namespace EnergoSolutions_03.Models.Agent;

public class UserData
{
    public Location Location { get; set; } = new();
    public Building Building { get; set; } = new();
    public Consumption Consumption { get; set; } = new();
    public Roof Roof { get; set; } = new();
    public Electrical Electrical { get; set; } = new();
}