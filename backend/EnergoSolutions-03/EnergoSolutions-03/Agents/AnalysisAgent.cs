using System.Text.Json;
using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.Models.Agent;

namespace EnergoSolutions_03.Agents;

public class AnalysisAgent : IAnalysisAgent
{
    private readonly IOpenAIService _openAI;
    private readonly IGeocodingService _geocodingService;
    private readonly ISummaryService _summaryService;

    public AnalysisAgent(IOpenAIService openAI, ISummaryService summaryService, IGeocodingService geocodingService)
    {
        _openAI = openAI;
        _summaryService = summaryService;
        _geocodingService = geocodingService;
    }
    
    public async Task<AgentResponse> Analyze(Session session)
    {
        var userData = session.UserData;
        
        // Získaj súhrn technických dát z SummaryService
        var geocodeResult = await _geocodingService.GeocodeAsync(userData.Location.Address);
        float lat = geocodeResult.Latitude;
        float lon = geocodeResult.Longitude;
        var technicalSummary = await _summaryService.BuildSummaryAsync(lat, lon);
        
        // Serializuj UserData do JSON
        var userDataJson = JsonSerializer.Serialize(userData, new JsonSerializerOptions { WriteIndented = true });
        
        // System message pre AI
        var systemMessage = @"You are an expert energy consultant specializing in renewable energy technologies for homes across the World. You will receive:
1. User data (building info, consumption, location) as JSON
2. Technical data summary (solar radiation, wind speed, climate)

Your task: Analyze the potential for three technologies (Solar, Wind, HeatPump) and return ONLY a valid JSON object with this exact structure:

{
  ""solar"": {
    ""score"": 0-100,
    ""reasoning"": ""brief explanation in Slovak""
  },
  ""wind"": {
    ""score"": 0-100,
    ""reasoning"": ""brief explanation in Slovak""
  },
  ""heatpump"": {
    ""score"": 0-100,
    ""reasoning"": ""brief explanation in Slovak""
  }
}

Scoring criteria:
- Solar: radiation quality (40 pts), roof area (30 pts), consumption (30 pts)
- Wind: avg speed (50 pts), building type (30 pts), locality (20 pts)
- HeatPump: climate (20 pts), insulation (20 pts), base score 60 pts

Be concise but specific in reasoning. Use Slovak language for reasoning text.";

        var userPrompt = $@"User Data:
{userDataJson}

Technical Summary:
{technicalSummary}

Analyze and return JSON only.";

        // Zavolaj OpenAI
        var aiResult = await _openAI.CreateResponseAsync(systemMessage, userPrompt, model: "gpt-4.1");
        
        // Parse AI response
        TechnologyScore solarScore, windScore, heatPumpScore;
        
        try
        {
            var aiJson = JsonDocument.Parse(aiResult);
            var root = aiJson.RootElement;
            
            solarScore = new TechnologyScore
            {
                Technology = "Solar",
                Score = root.GetProperty("solar").GetProperty("score").GetInt32(),
                Reasoning = root.GetProperty("solar").GetProperty("reasoning").GetString() ?? ""
            };
            
            windScore = new TechnologyScore
            {
                Technology = "Wind",
                Score = root.GetProperty("wind").GetProperty("score").GetInt32(),
                Reasoning = root.GetProperty("wind").GetProperty("reasoning").GetString() ?? ""
            };
            
            heatPumpScore = new TechnologyScore
            {
                Technology = "HeatPump",
                Score = root.GetProperty("heatpump").GetProperty("score").GetInt32(),
                Reasoning = root.GetProperty("heatpump").GetProperty("reasoning").GetString() ?? ""
            };
        }
        catch (Exception ex)
        {
            // Fallback ak AI nevrátil validný JSON
            return new AgentResponse
            {
                Message = $"❌ Chyba pri analýze dát: {ex.Message}\n\nAI odpoveď:\n{aiResult}",
                IsComplete = false,
                Progress = 50
            };
        }
        
        // Ulož výsledky do session
        session.AnalysisResults = new AnalysisResults
        {
            SolarPotential = solarScore,
            WindPotential = windScore,
            HeatPumpPotential = heatPumpScore,
            RecommendedTechnologies = GetRecommendations(solarScore, windScore, heatPumpScore)
        };

        var message = $@"
📊 **Analýza dokončená!**

Na základe vašej lokality ({userData.Location.Address}) a technických údajov:

☀️ **Solárny potenciál: {solarScore.Score}/100**
{solarScore.Reasoning}

💨 **Veterný potenciál: {windScore.Score}/100**
{windScore.Reasoning}

🔥 **Tepelné čerpadlo: {heatPumpScore.Score}/100**
{heatPumpScore.Reasoning}

Teraz vypočítam optimálnu zostavu pre váš dom...
";

        return new AgentResponse
        {
            Message = message,
            IsComplete = true,
            Progress = 50
        };
    }

    private List<string> GetRecommendations(TechnologyScore solar, TechnologyScore wind, TechnologyScore heatPump)
    {
        var recommendations = new List<string>();

        if (solar.Score >= 70)
            recommendations.Add("solar");
        if (wind.Score >= 60)
            recommendations.Add("wind");
        if (heatPump.Score >= 70)
            recommendations.Add("heatpump");

        if (recommendations.Count == 0)
        {
            // Odporuč aspoň niečo
            if (solar.Score >= 50)
                recommendations.Add("solar");
            else if (heatPump.Score >= 50)
                recommendations.Add("heatpump");
        }

        return recommendations;
    }
}