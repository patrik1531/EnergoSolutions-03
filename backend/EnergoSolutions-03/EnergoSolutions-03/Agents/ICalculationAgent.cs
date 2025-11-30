using EnergoSolutions_03.Models.Agent;

namespace EnergoSolutions_03.Agents;

public interface ICalculationAgent
{
    Task<AgentResponse> Calculate(Session session);
}