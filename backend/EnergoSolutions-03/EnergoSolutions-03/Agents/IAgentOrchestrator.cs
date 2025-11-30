using EnergoSolutions_03.Models.Agent;

namespace EnergoSolutions_03.Agents;

public interface IAgentOrchestrator
{
    Task<string> StartNewSession();
    Task<AgentResponse> ProcessMessage(string sessionId, string message);
    Task<SessionStatus> GetSessionStatus(string sessionId);
}