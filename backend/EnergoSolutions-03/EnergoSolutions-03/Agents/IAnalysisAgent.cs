using EnergoSolutions_03.Models.Agent;

namespace EnergoSolutions_03.Agents;

public interface IAnalysisAgent
{
    Task<AgentResponse> Analyze(Session session);
}