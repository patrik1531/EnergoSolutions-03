using EnergoSolutions_03.Models.Agent;

namespace EnergoSolutions_03.Agents;

public interface IReportAgent
{
    Task<AgentResponse> GenerateReport(Session session);
}