namespace EnergoSolutions_03.Models.Agent;

public class AgentResponse
{
    public string Message { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public int Progress { get; set; }
}