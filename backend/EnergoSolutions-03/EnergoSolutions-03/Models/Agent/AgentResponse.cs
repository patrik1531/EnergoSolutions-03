namespace EnergoSolutions_03.Models.Agent;

public class AgentResponse
{
    public string Message { get; set; }
    public bool IsComplete { get; set; }
    public string SessionId { get; set; }
    public int Progress { get; set; }
}