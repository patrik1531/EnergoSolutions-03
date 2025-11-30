namespace EnergoSolutions_03.Models.Agent;

public class Session
{
    public string SessionId { get; set; } = string.Empty;
    public AgentType CurrentAgent { get; set; } = AgentType.DataCollector;
    public UserData UserData { get; set; } = new();
    public TechnicalData TechnicalData { get; set; } = new();
    public AnalysisResults AnalysisResults { get; set; } = new();
    public CalculationResults Calculations { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}