namespace EnergoSolutions_03.Models.Agent;

public class Session
{
    public string SessionId { get; set; }
    public AgentType CurrentAgent { get; set; } = AgentType.DataCollector;
    public UserData UserData { get; set; }
    public TechnicalData TechnicalData { get; set; }
    public AnalysisResults AnalysisResults { get; set; }
    public CalculationResults Calculations { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}