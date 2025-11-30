using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.Models.Agent;

namespace EnergoSolutions_03.Agents;

public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IDataCollectorAgent _dataCollector;
    private readonly IAnalysisAgent _analysisAgent;
    private readonly ICalculationAgent _calculationAgent;
    private readonly IReportAgent _reportAgent;
    private readonly ISessionManager _sessionManager;

    public AgentOrchestrator(
        IDataCollectorAgent dataCollector,
        IAnalysisAgent analysisAgent,
        ICalculationAgent calculationAgent,
        IReportAgent reportAgent,
        ISessionManager sessionManager)
    {
        _dataCollector = dataCollector;
        _analysisAgent = analysisAgent;
        _calculationAgent = calculationAgent;
        _reportAgent = reportAgent;
        _sessionManager = sessionManager;
    }

    public async Task<string> StartNewSession()
    {
        return await _sessionManager.CreateSession();
    }

    public async Task<AgentResponse> ProcessMessage(string sessionId, string message)
    {
        var session = await _sessionManager.GetSession(sessionId);
        
        switch (session.CurrentAgent)
        {
            case AgentType.DataCollector:
                var collectorResponse = await _dataCollector.ProcessMessage(session, message);
                
                if (collectorResponse.IsComplete)
                {
                    // Prejdi na Analysis Agent
                    session.CurrentAgent = AgentType.Analysis;
                    await _sessionManager.UpdateSession(session);
                    
                    // Automaticky spusti analýzu
                    var analysisResponse = await _analysisAgent.Analyze(session);
                    
                    if (analysisResponse.IsComplete)
                    {
                        session.CurrentAgent = AgentType.Calculation;
                        await _sessionManager.UpdateSession(session);
                        
                        // Spusti výpočty
                        var calcResponse = await _calculationAgent.Calculate(session);
                        
                        if (calcResponse.IsComplete)
                        {
                            session.CurrentAgent = AgentType.Report;
                            await _sessionManager.UpdateSession(session);
                            
                            // Vygeneruj finálny report
                            return await _reportAgent.GenerateReport(session);
                        }
                        return calcResponse;
                    }
                    return analysisResponse;
                }
                return collectorResponse;

            case AgentType.Analysis:
                return await _analysisAgent.Analyze(session);

            case AgentType.Calculation:
                return await _calculationAgent.Calculate(session);

            case AgentType.Report:
                return await _reportAgent.GenerateReport(session);

            default:
                return new AgentResponse 
                { 
                    Message = "Chyba: Neznámy stav konverzácie",
                    IsComplete = true 
                };
        }
    }

    public async Task<SessionStatus> GetSessionStatus(string sessionId)
    {
        var session = await _sessionManager.GetSession(sessionId);
        return new SessionStatus
        {
            SessionId = sessionId,
            CurrentAgent = session.CurrentAgent.ToString(),
            Progress = CalculateProgress(session),
            CollectedData = session.UserData,
            TechnicalData = session.TechnicalData,
            Analysis = session.AnalysisResults,
            Calculations = session.Calculations
        };
    }

    private int CalculateProgress(Session session)
    {
        return session.CurrentAgent switch
        {
            AgentType.DataCollector => 25,
            AgentType.Analysis => 50,
            AgentType.Calculation => 75,
            AgentType.Report => 100,
            _ => 0
        };
    }
}