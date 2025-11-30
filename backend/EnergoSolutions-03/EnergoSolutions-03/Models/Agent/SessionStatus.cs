namespace EnergoSolutions_03.Models.Agent;

public class SessionStatus
{
    public string SessionId { get; set; }
    public string CurrentAgent { get; set; }
    public int Progress { get; set; }
    public UserData CollectedData { get; set; }
    public TechnicalData TechnicalData { get; set; }
    public AnalysisResults Analysis { get; set; }
    public CalculationResults Calculations { get; set; }
}