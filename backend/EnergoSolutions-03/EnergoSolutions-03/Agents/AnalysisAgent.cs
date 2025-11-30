using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.Models.Agent;

namespace EnergoSolutions_03.Agents;

public class AnalysisAgent : IAnalysisAgent
{
    private readonly IOpenAIService _openAI;

    public AnalysisAgent(IOpenAIService openAi)
    {
        _openAI = openAi;
    }
    
    public async Task<AgentResponse> Analyze(Session session)
    {
        var userData = session.UserData;
        var techData = session.TechnicalData;
        
        // Analyzuj každú technológiu
        var solarScore = AnalyzeSolar(userData, techData);
        var windScore = AnalyzeWind(userData, techData);
        var heatPumpScore = AnalyzeHeatPump(userData, techData);

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

    private TechnologyScore AnalyzeSolar(UserData userData, TechnicalData techData)
    {
        var score = 0;
        var factors = new List<string>();

        // Slnečné žiarenie (0-40 bodov)
        var solarRadiation = techData.SolarResource.YearlyKwhPerKwp;
        if (solarRadiation > 1100)
        {
            score += 40;
            factors.Add($"Výborné slnečné žiarenie ({solarRadiation} kWh/kWp ročne)");
        }
        else if (solarRadiation > 950)
        {
            score += 30;
            factors.Add($"Dobré slnečné žiarenie ({solarRadiation} kWh/kWp ročne)");
        }
        else if (solarRadiation > 850)
        {
            score += 20;
            factors.Add($"Priemerné slnečné žiarenie ({solarRadiation} kWh/kWp ročne)");
        }
        else
        {
            score += 10;
            factors.Add($"Nízke slnečné žiarenie ({solarRadiation} kWh/kWp ročne)");
        }

        // Strecha (0-30 bodov)
        if (userData.Building.BuildingType == "family_house" && userData.Roof.RoofAreaM2 > 0)
        {
            if (userData.Roof.RoofAreaM2 >= 50)
            {
                score += 30;
                factors.Add($"Veľká využiteľná plocha strechy ({userData.Roof.RoofAreaM2} m²)");
            }
            else if (userData.Roof.RoofAreaM2 >= 30)
            {
                score += 20;
                factors.Add($"Dostatočná plocha strechy ({userData.Roof.RoofAreaM2} m²)");
            }
            else
            {
                score += 10;
                factors.Add($"Malá plocha strechy ({userData.Roof.RoofAreaM2} m²)");
            }
        }
        else if (userData.Building.BuildingType == "apartment")
        {
            score += 0;
            factors.Add("Byt - obmedzené možnosti inštalácie");
        }

        // Spotreba (0-30 bodov)
        if (userData.Consumption.ElectricityKwhYear > 4000)
        {
            score += 30;
            factors.Add("Vysoká spotreba - FV sa rýchlo vráti");
        }
        else if (userData.Consumption.ElectricityKwhYear > 2500)
        {
            score += 20;
            factors.Add("Stredná spotreba");
        }
        else
        {
            score += 10;
            factors.Add("Nízka spotreba");
        }

        return new TechnologyScore
        {
            Technology = "Solar",
            Score = score,
            Reasoning = string.Join(", ", factors)
        };
    }

    private TechnologyScore AnalyzeWind(UserData userData, TechnicalData techData)
    {
        var score = 0;
        var factors = new List<string>();

        // Priemerná rýchlosť vetra (0-50 bodov)
        var windSpeed = techData.WindData.AverageSpeed;
        if (windSpeed > 6)
        {
            score += 50;
            factors.Add($"Výborný vietor ({windSpeed:F1} m/s)");
        }
        else if (windSpeed > 4.5)
        {
            score += 30;
            factors.Add($"Dobrý vietor ({windSpeed:F1} m/s)");
        }
        else if (windSpeed > 3.5)
        {
            score += 15;
            factors.Add($"Slabý vietor ({windSpeed:F1} m/s)");
        }
        else
        {
            score += 0;
            factors.Add($"Nedostatočný vietor ({windSpeed:F1} m/s)");
        }

        // Typ budovy (0-30 bodov)
        if (userData.Building.BuildingType == "family_house")
        {
            score += 30;
            factors.Add("Rodinný dom - možná inštalácia");
        }
        else
        {
            score += 0;
            factors.Add("Byt/budova - ťažká inštalácia turbíny");
        }

        // Lokalita (0-20 bodov) - odhadujeme podľa vetra
        if (windSpeed > 5)
        {
            score += 20;
            factors.Add("Otvorená lokalita");
        }

        return new TechnologyScore
        {
            Technology = "Wind",
            Score = Math.Min(score, 100),
            Reasoning = string.Join(", ", factors)
        };
    }

    private TechnologyScore AnalyzeHeatPump(UserData userData, TechnicalData techData)
    {
        var score = 60; // Základné skóre - tepelné čerpadlá sú všeobecne dobré
        var factors = new List<string>();

        // Teplota (0-20 bodov)
        var avgTemp = techData.ClimateData.YearAverageTemp;
        if (avgTemp > 10)
        {
            score += 20;
            factors.Add($"Mierna klíma (priemer {avgTemp:F1}°C)");
        }
        else if (avgTemp > 7)
        {
            score += 15;
            factors.Add($"Chladnejšia klíma (priemer {avgTemp:F1}°C)");
        }
        else
        {
            score += 10;
            factors.Add($"Studená klíma (priemer {avgTemp:F1}°C) - nižšia účinnosť");
        }

        // Izolácia (0-20 bodov)
        if (userData.Building.InsulationLevel == "good")
        {
            score += 20;
            factors.Add("Dobrá izolácia - ideálne pre TČ");
        }
        else if (userData.Building.InsulationLevel == "average")
        {
            score += 10;
            factors.Add("Priemerná izolácia");
        }
        else
        {
            score += 0;
            factors.Add("Zlá izolácia - najprv zatepliť");
        }

        // Aktuálne kúrenie
        if (userData.Consumption.HeatingFuel == "electricity" || userData.Consumption.HeatingFuel == "gas")
        {
            factors.Add("Jednoduché nahradenie súčasného systému");
        }

        return new TechnologyScore
        {
            Technology = "HeatPump",
            Score = Math.Min(score, 100),
            Reasoning = string.Join(", ", factors)
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