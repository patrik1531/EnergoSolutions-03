using System.Text.Json;
using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.Models.Agent;

namespace EnergoSolutions_03.Agents;

public class CalculationAgent : ICalculationAgent
{
    private readonly IOpenAIService _openAI;

    public CalculationAgent(IOpenAIService openAI)
    {
        _openAI = openAI;
    }

    public async Task<AgentResponse> Calculate(Session session)
    {
        var userData = session.UserData;
        var techData = session.TechnicalData;
        var analysis = session.AnalysisResults;

        // Serializuj dáta do JSON pre AI
        var userDataJson = JsonSerializer.Serialize(userData, new JsonSerializerOptions { WriteIndented = true });
        var techDataJson = JsonSerializer.Serialize(techData, new JsonSerializerOptions { WriteIndented = true });
        var analysisJson = JsonSerializer.Serialize(analysis, new JsonSerializerOptions { WriteIndented = true });

        // System message pre AI
        var systemMessage = @"You are an expert renewable energy system calculator for Slovakia. You will receive:
1. UserData (building, consumption, location) as JSON
2. TechnicalData (solar radiation, wind, climate) as JSON
3. AnalysisResults (recommended technologies) as JSON

Your task: Calculate detailed economics for each recommended technology and return ONLY a valid JSON object with this structure:

{
  ""solarSystem"": {
    ""technology"": ""Fotovoltika"",
    ""systemSize"": ""X.X kWp"",
    ""numberOfPanels"": 10,
    ""yearlyProduction"": ""XXXX kWh"",
    ""installationCost"": 15000.0,
    ""yearlySavings"": 800.0,
    ""paybackYears"": 18.75,
    ""roi"": 66.67,
    ""details"": {
      ""Vlastná spotreba"": ""3500 kWh/rok"",
      ""Predaj do siete"": ""500 kWh/rok"",
      ""Pokrytie spotreby"": ""85%""
    }
  },
  ""windSystem"": { ... same structure ... },
  ""heatPumpSystem"": { ... same structure ... },
  ""combinedSystem"": { ... same structure with synergy benefits ... }
}

CALCULATION RULES:
**Solar (Fotovoltika):**
- Optimal kWp = min(yearlyConsumption/1000, roofArea*0.7/2*0.4)
- Panels = kWp / 0.4 (400Wp panels)
- Yearly production = kWp * yearlyKwhPerKwp
- Installation cost = kWp * 1500 €/kWp
- Self-consumption = min(production*0.7, consumption)
- Grid export = production - self-consumption
- Yearly savings = self-consumption*0.20 + grid_export*0.05
- Payback = cost / savings
- ROI over 25 years = (savings*25 - cost) / cost * 100

**Wind (Veterná turbína):**
- System size: 5 kW turbine for homes
- Capacity factor: >6 m/s=30%, >5=20%, >4=15%, else 10%
- Yearly production = 5 * 8760 * capacity_factor
- Installation cost = 15000 €
- Yearly savings = production * 0.18
- Payback = cost / savings
- ROI over 20 years

**HeatPump (Tepelné čerpadlo):**
- Heating demand = heatedArea * specific_demand (good=50, average=100, poor=150 kWh/m²/year)
- HP size in kW = demand / 2000
- COP = avgTemp>8 ? 3.5 : 3.0
- Current heating cost by fuel: gas=0.08, electricity=0.18, wood=0.05 €/kWh
- HP consumption = demand / COP
- HP cost = consumption * 0.18
- Yearly savings = current_cost - hp_cost
- Installation = size_kW * 2500 €/kW
- Payback & ROI over 15 years

**Combined System:**
- Only combine recommended technologies
- Apply 10% discount on solar, 5% on heatpump when combined
- Add 10% synergy bonus to yearly savings (solar powers heatpump)
- Calculate combined payback & ROI over 20 years

Return ONLY technologies from recommendedTechnologies list. Use null for non-recommended ones.
All text in details must be in Slovak.";

        var userPrompt = $@"UserData:
{userDataJson}

TechnicalData:
{techDataJson}

AnalysisResults:
{analysisJson}

Calculate economics and return JSON only.";

        // Zavolaj OpenAI
        var aiResult = await _openAI.CreateResponseAsync(systemMessage, userPrompt, model: "gpt-4.1");

        // Parse AI response
        CalculationResults calculations;
        
        try
        {
            var aiJson = JsonDocument.Parse(aiResult);
            var root = aiJson.RootElement;
            
            calculations = new CalculationResults();

            // Parse Solar
            if (root.TryGetProperty("solarSystem", out var solarJson) && solarJson.ValueKind != JsonValueKind.Null)
            {
                calculations.SolarSystem = ParseSystemCalculation(solarJson);
            }

            // Parse Wind
            if (root.TryGetProperty("windSystem", out var windJson) && windJson.ValueKind != JsonValueKind.Null)
            {
                calculations.WindSystem = ParseSystemCalculation(windJson);
            }

            // Parse HeatPump
            if (root.TryGetProperty("heatPumpSystem", out var hpJson) && hpJson.ValueKind != JsonValueKind.Null)
            {
                calculations.HeatPumpSystem = ParseSystemCalculation(hpJson);
            }

            // Parse Combined
            if (root.TryGetProperty("combinedSystem", out var combJson) && combJson.ValueKind != JsonValueKind.Null)
            {
                calculations.CombinedSystem = ParseSystemCalculation(combJson);
            }
        }
        catch (Exception ex)
        {
            // Fallback ak AI nevrátil validný JSON
            return new AgentResponse
            {
                Message = $"❌ Chyba pri výpočte ekonomiky: {ex.Message}\n\nAI odpoveď:\n{aiResult}",
                IsComplete = false,
                Progress = 75
            };
        }

        session.Calculations = calculations;

        var message = FormatCalculationResults(calculations);

        return new AgentResponse
        {
            Message = message,
            IsComplete = true,
            Progress = 75
        };
    }

    private SystemCalculation ParseSystemCalculation(JsonElement json)
    {
        var calc = new SystemCalculation
        {
            Technology = json.GetProperty("technology").GetString() ?? "",
            SystemSize = json.GetProperty("systemSize").GetString() ?? "",
            YearlyProduction = json.GetProperty("yearlyProduction").GetString() ?? "",
            InstallationCost = json.GetProperty("installationCost").GetDouble(),
            YearlySavings = json.GetProperty("yearlySavings").GetDouble(),
            PaybackYears = json.GetProperty("paybackYears").GetDouble(),
            ROI = json.GetProperty("roi").GetDouble()
        };

        // Parse numberOfPanels (optional, only for solar)
        if (json.TryGetProperty("numberOfPanels", out var panelsJson))
        {
            calc.NumberOfPanels = panelsJson.GetInt32();
        }

        // Parse details dictionary
        if (json.TryGetProperty("details", out var detailsJson))
        {
            calc.Details = new Dictionary<string, object>();
            foreach (var prop in detailsJson.EnumerateObject())
            {
                calc.Details[prop.Name] = prop.Value.GetString() ?? "";
            }
        }

        return calc;
    }

    private string FormatCalculationResults(CalculationResults calculations)
    {
        var message = "💰 **Ekonomická kalkulácia:**\n\n";

        if (calculations.SolarSystem != null)
        {
            message += FormatSystem(calculations.SolarSystem);
        }

        if (calculations.WindSystem != null)
        {
            message += FormatSystem(calculations.WindSystem);
        }

        if (calculations.HeatPumpSystem != null)
        {
            message += FormatSystem(calculations.HeatPumpSystem);
        }

        if (calculations.CombinedSystem != null)
        {
            message += "\n🎯 **ODPORÚČANÁ KOMBINÁCIA:**\n";
            message += FormatSystem(calculations.CombinedSystem);
        }

        message += "\nPripravujem finálny report s detailnými odporúčaniami...";

        return message;
    }

    private string FormatSystem(SystemCalculation calc)
    {
        return $@"
**{calc.Technology}** ({calc.SystemSize})
- Investícia: {calc.InstallationCost:F0} €
- Ročná úspora: {calc.YearlySavings:F0} €
- Návratnosť: {calc.PaybackYears:F1} rokov
- ROI (20 rokov): {calc.ROI:F0}%

";
    }
}