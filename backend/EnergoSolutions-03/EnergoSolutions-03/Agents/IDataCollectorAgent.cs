using EnergoSolutions_03.Models.Agent;

namespace EnergoSolutions_03.Agents;

public interface IDataCollectorAgent
{
    Task<AgentResponse> ProcessMessage(Session session, string message);
}