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

            var calculations = new CalculationResults();

            // Vypočítaj pre každú odporúčanú technológiu
            if (analysis.RecommendedTechnologies.Contains("solar"))
            {
                calculations.SolarSystem = CalculateSolar(userData, techData);
            }

            if (analysis.RecommendedTechnologies.Contains("wind"))
            {
                calculations.WindSystem = CalculateWind(userData, techData);
            }

            if (analysis.RecommendedTechnologies.Contains("heatpump"))
            {
                calculations.HeatPumpSystem = CalculateHeatPump(userData, techData);
            }

            // Vypočítaj kombinovanú zostavu
            calculations.CombinedSystem = CalculateCombined(calculations, userData);

            session.Calculations = calculations;

            var message = FormatCalculationResults(calculations);

            return new AgentResponse
            {
                Message = message,
                IsComplete = true,
                Progress = 75
            };
        }

        private SystemCalculation CalculateSolar(UserData userData, TechnicalData techData)
        {
            var calc = new SystemCalculation { Technology = "Fotovoltika" };

            // Určenie veľkosti systému
            var roofArea = userData.Roof?.RoofAreaM2 ?? 50;
            var maxPanels = (int)(roofArea * 0.7 / 2); // 2m² na panel, 70% využiteľnosť
            var yearlyConsumption = userData.Consumption.ElectricityKwhYear ?? 3500;
            
            // Optimálny výkon
            var optimalKwp = Math.Min(yearlyConsumption / 1000.0, maxPanels * 0.4); // 400Wp panely
            calc.SystemSize = $"{optimalKwp:F1} kWp";
            calc.NumberOfPanels = (int)(optimalKwp / 0.4);

            // Produkcia
            var yearlyProduction = optimalKwp * techData.SolarResource.YearlyKwhPerKwp;
            calc.YearlyProduction = $"{yearlyProduction:F0} kWh";
            
            // Náklady (približné ceny)
            calc.InstallationCost = optimalKwp * 1500; // 1500€/kWp
            
            // Úspory
            var selfConsumption = Math.Min(yearlyProduction * 0.7, yearlyConsumption); // 70% vlastná spotreba
            var gridExport = yearlyProduction - selfConsumption;
            
            calc.YearlySavings = selfConsumption * 0.20 + gridExport * 0.05; // 0.20€/kWh úspora, 0.05€/kWh výkup
            
            // ROI
            calc.PaybackYears = calc.InstallationCost / calc.YearlySavings;
            calc.ROI = (calc.YearlySavings * 25 - calc.InstallationCost) / calc.InstallationCost * 100;

            calc.Details = new Dictionary<string, object>
            {
                ["Vlastná spotreba"] = $"{selfConsumption:F0} kWh/rok",
                ["Predaj do siete"] = $"{gridExport:F0} kWh/rok",
                ["Pokrytie spotreby"] = $"{(selfConsumption/yearlyConsumption*100):F0}%"
            };

            return calc;
        }

        private SystemCalculation CalculateWind(UserData userData, TechnicalData techData)
        {
            var calc = new SystemCalculation { Technology = "Veterná turbína" };

            var windSpeed = techData.WindData.AverageSpeed;
            
            // Malá turbína 5kW pre rodinný dom
            calc.SystemSize = "5 kW";
            
            // Kapacitný faktor podľa rýchlosti vetra
            double capacityFactor = windSpeed switch
            {
                > 6 => 0.30,
                > 5 => 0.20,
                > 4 => 0.15,
                _ => 0.10
            };

            var yearlyProduction = 5 * 8760 * capacityFactor; // kW * hodiny * faktor
            calc.YearlyProduction = $"{yearlyProduction:F0} kWh";
            
            calc.InstallationCost = 15000; // 5kW turbína
            calc.YearlySavings = yearlyProduction * 0.18; // 0.18€/kWh
            calc.PaybackYears = calc.InstallationCost / calc.YearlySavings;
            calc.ROI = (calc.YearlySavings * 20 - calc.InstallationCost) / calc.InstallationCost * 100;

            calc.Details = new Dictionary<string, object>
            {
                ["Priemerný vietor"] = $"{windSpeed:F1} m/s",
                ["Kapacitný faktor"] = $"{capacityFactor*100:F0}%",
                ["Ročná produkcia"] = $"{yearlyProduction:F0} kWh"
            };

            return calc;
        }

        private SystemCalculation CalculateHeatPump(UserData userData, TechnicalData techData)
        {
            var calc = new SystemCalculation { Technology = "Tepelné čerpadlo" };

            var heatedArea = userData.Building.HeatedAreaM2 ?? 150;
            var heatingDemand = CalculateHeatingDemand(heatedArea, userData.Building.InsulationLevel);
            
            // Veľkosť TČ
            var heatPumpSize = heatingDemand / 2000; // približne 2000 hodín vykurovania
            calc.SystemSize = $"{heatPumpSize:F0} kW";
            
            // COP podľa teploty
            var avgTemp = techData.ClimateData.YearAverageTemp;
            var cop = avgTemp > 8 ? 3.5 : 3.0;
            
            // Spotreba a úspory
            var currentHeatingCost = CalculateCurrentHeatingCost(userData, heatingDemand);
            var heatPumpConsumption = heatingDemand / cop;
            var heatPumpCost = heatPumpConsumption * 0.18; // 0.18€/kWh

            calc.YearlyProduction = $"COP {cop:F1}";
            calc.InstallationCost = heatPumpSize * 2500; // 2500€/kW
            calc.YearlySavings = currentHeatingCost - heatPumpCost;
            calc.PaybackYears = calc.InstallationCost / calc.YearlySavings;
            calc.ROI = (calc.YearlySavings * 15 - calc.InstallationCost) / calc.InstallationCost * 100;

            calc.Details = new Dictionary<string, object>
            {
                ["Vykurovacia záťaž"] = $"{heatingDemand:F0} kWh/rok",
                ["Súčasné náklady"] = $"{currentHeatingCost:F0} €/rok",
                ["Nové náklady"] = $"{heatPumpCost:F0} €/rok",
                ["Úspora"] = $"{calc.YearlySavings:F0} €/rok"
            };

            return calc;
        }

        private SystemCalculation CalculateCombined(CalculationResults calculations, UserData userData)
        {
            var combined = new SystemCalculation { Technology = "Kombinovaný systém" };

            double totalCost = 0;
            double totalSavings = 0;
            var components = new List<string>();

            if (calculations.SolarSystem != null)
            {
                totalCost += calculations.SolarSystem.InstallationCost * 0.9; // 10% zľava pri kombinácii
                totalSavings += calculations.SolarSystem.YearlySavings;
                components.Add($"FV {calculations.SolarSystem.SystemSize}");
            }

            if (calculations.HeatPumpSystem != null)
            {
                totalCost += calculations.HeatPumpSystem.InstallationCost * 0.95;
                totalSavings += calculations.HeatPumpSystem.YearlySavings;
                components.Add($"TČ {calculations.HeatPumpSystem.SystemSize}");
            }

            combined.SystemSize = string.Join(" + ", components);
            combined.InstallationCost = totalCost;
            combined.YearlySavings = totalSavings * 1.1; // 10% synergia
            combined.PaybackYears = totalCost / combined.YearlySavings;
            combined.ROI = (combined.YearlySavings * 20 - totalCost) / totalCost * 100;

            combined.Details = new Dictionary<string, object>
            {
                ["Synergia"] = "FV napája TČ = lacnejšie vykurovanie",
                ["Celková investícia"] = $"{totalCost:F0} €",
                ["Ročná úspora"] = $"{combined.YearlySavings:F0} €"
            };

            return combined;
        }

        private double CalculateHeatingDemand(int area, string insulation)
        {
            var specificDemand = insulation switch
            {
                "good" => 50,    // kWh/m²/rok
                "average" => 100,
                "poor" => 150,
                _ => 100
            };
            return area * specificDemand;
        }

        private double CalculateCurrentHeatingCost(UserData userData, double heatingDemand)
        {
            return userData.Consumption.HeatingFuel switch
            {
                "gas" => heatingDemand * 0.08,      // 0.08€/kWh plyn
                "electricity" => heatingDemand * 0.18, // 0.18€/kWh elektrina
                "wood" => heatingDemand * 0.05,     // 0.05€/kWh drevo
                _ => heatingDemand * 0.10
            };
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
• Investícia: {calc.InstallationCost:F0} €
• Ročná úspora: {calc.YearlySavings:F0} €
• Návratnosť: {calc.PaybackYears:F1} rokov
• ROI (20 rokov): {calc.ROI:F0}%

";
        }
    }