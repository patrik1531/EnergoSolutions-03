namespace EnergoSolutions_03.Models.Agent;

public class SessionStatus
{
    public string SessionId { get; set; } = string.Empty;
    public string CurrentAgent { get; set; } = string.Empty;
    public int Progress { get; set; }
    public UserData CollectedData { get; set; } = new();
    public TechnicalData TechnicalData { get; set; } = new();
    public AnalysisResults Analysis { get; set; } = new();
    public CalculationResults Calculations { get; set; } = new();
}