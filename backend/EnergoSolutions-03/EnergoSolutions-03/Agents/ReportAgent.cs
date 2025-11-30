using System.Text;
using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.Models.Agent;

namespace EnergoSolutions_03.Agents;

public class ReportAgent : IReportAgent
    {
        private readonly IOpenAIService _openAI;

        public ReportAgent(IOpenAIService openAI)
        {
            _openAI = openAI;
        }

        public async Task<AgentResponse> GenerateReport(Session session)
        {
            var report = new StringBuilder();
            
            report.AppendLine("# 🌱 PERSONALIZOVANÝ ENERGETICKÝ PLÁN\n");
            report.AppendLine($"**Pre:** {session.UserData.Location.Address}");
            report.AppendLine($"**Typ objektu:** {GetBuildingTypeText(session.UserData.Building.BuildingType)}");
            report.AppendLine($"**Vykurovaná plocha:** {session.UserData.Building.HeatedAreaM2} m²\n");
            report.AppendLine("---\n");

            // Súhrn analýzy
            report.AppendLine("## 📊 Súhrn analýzy\n");
            report.AppendLine(GenerateAnalysisSummary(session));

            // Odporúčania
            report.AppendLine("\n## 💡 Naše odporúčania\n");
            report.AppendLine(GenerateRecommendations(session));

            // Ekonomika
            report.AppendLine("\n## 💰 Ekonomická analýza\n");
            report.AppendLine(GenerateEconomicAnalysis(session));

            // Implementačný plán
            report.AppendLine("\n## 📅 Implementačný plán\n");
            report.AppendLine(GenerateImplementationPlan(session));

            // Záver
            report.AppendLine("\n## ✅ Záver\n");
            report.AppendLine(await GenerateConclusion(session));

            // Kontakt
            report.AppendLine("\n---");
            report.AppendLine("*Pre detailnú ponuku a realizáciu kontaktujte našich špecialistov.*");
            report.AppendLine("📞 **0800 123 456** | 📧 **info@greenenergy.sk**");

            return new AgentResponse
            {
                Message = report.ToString(),
                IsComplete = true,
                Progress = 100
            };
        }

        private string GenerateAnalysisSummary(Session session)
        {
            var analysis = session.AnalysisResults;
            var summary = new StringBuilder();

            summary.AppendLine("Vaša lokalita má nasledujúci potenciál pre obnoviteľné zdroje:");
            summary.AppendLine();

            if (analysis.SolarPotential.Score >= 70)
            {
                summary.AppendLine($"☀️ **Fotovoltika:** VÝBORNÝ potenciál ({analysis.SolarPotential.Score}/100)");
                summary.AppendLine($"   *{analysis.SolarPotential.Reasoning}*");
            }
            else if (analysis.SolarPotential.Score >= 50)
            {
                summary.AppendLine($"☀️ **Fotovoltika:** Dobrý potenciál ({analysis.SolarPotential.Score}/100)");
            }
            else
            {
                summary.AppendLine($"☀️ **Fotovoltika:** Obmedzený potenciál ({analysis.SolarPotential.Score}/100)");
            }

            summary.AppendLine();

            if (analysis.WindPotential.Score >= 60)
            {
                summary.AppendLine($"💨 **Veterná energia:** Vhodná lokalita ({analysis.WindPotential.Score}/100)");
                summary.AppendLine($"   *{analysis.WindPotential.Reasoning}*");
            }
            else
            {
                summary.AppendLine($"💨 **Veterná energia:** Nevhodné podmienky ({analysis.WindPotential.Score}/100)");
            }

            summary.AppendLine();

            if (analysis.HeatPumpPotential.Score >= 70)
            {
                summary.AppendLine($"🔥 **Tepelné čerpadlo:** ODPORÚČANÉ ({analysis.HeatPumpPotential.Score}/100)");
                summary.AppendLine($"   *{analysis.HeatPumpPotential.Reasoning}*");
            }
            else
            {
                summary.AppendLine($"🔥 **Tepelné čerpadlo:** Možná inštalácia ({analysis.HeatPumpPotential.Score}/100)");
            }

            return summary.ToString();
        }

        private string GenerateRecommendations(Session session)
        {
            var calculations = session.Calculations;
            var recommendations = new StringBuilder();

            // Najlepšia jednotlivá technológia
            var bestSingle = GetBestSingleSystem(calculations);
            if (bestSingle != null)
            {
                recommendations.AppendLine($"### 🥇 Najlepšia jednotlivá technológia: **{bestSingle.Technology}**\n");
                recommendations.AppendLine($"- Veľkosť systému: {bestSingle.SystemSize}");
                recommendations.AppendLine($"- Investícia: **{bestSingle.InstallationCost:F0} €**");
                recommendations.AppendLine($"- Ročná úspora: **{bestSingle.YearlySavings:F0} €**");
                recommendations.AppendLine($"- Návratnosť: **{bestSingle.PaybackYears:F1} rokov**");
                recommendations.AppendLine($"- Výnos za 20 rokov: **{(bestSingle.YearlySavings * 20 - bestSingle.InstallationCost):F0} €**");
            }

            // Kombinovaný systém
            if (calculations.CombinedSystem != null && calculations.CombinedSystem.ROI > 0)
            {
                recommendations.AppendLine($"\n### 🎯 Optimálna kombinácia: **{calculations.CombinedSystem.SystemSize}**\n");
                recommendations.AppendLine("**Výhody kombinovaného riešenia:**");
                recommendations.AppendLine("- Maximálna energetická nezávislosť");
                recommendations.AppendLine("- Synergia technológií (FV napája TČ)");
                recommendations.AppendLine("- Celoročná úspora");
                recommendations.AppendLine($"- Celková investícia: **{calculations.CombinedSystem.InstallationCost:F0} €**");
                recommendations.AppendLine($"- Celková ročná úspora: **{calculations.CombinedSystem.YearlySavings:F0} €**");
                recommendations.AppendLine($"- Návratnosť: **{calculations.CombinedSystem.PaybackYears:F1} rokov**");
            }

            return recommendations.ToString();
        }

        private string GenerateEconomicAnalysis(Session session)
        {
            var calc = session.Calculations;
            var economics = new StringBuilder();

            economics.AppendLine("### 📈 Porovnanie investičných možností\n");
            economics.AppendLine("| Technológia | Investícia | Ročná úspora | Návratnosť | ROI (20r) |");
            economics.AppendLine("|-------------|------------|--------------|------------|-----------|");

            if (calc.SolarSystem != null)
            {
                economics.AppendLine($"| Fotovoltika | {calc.SolarSystem.InstallationCost:F0} € | " +
                    $"{calc.SolarSystem.YearlySavings:F0} € | {calc.SolarSystem.PaybackYears:F1} r | " +
                    $"{calc.SolarSystem.ROI:F0}% |");
            }

            if (calc.WindSystem != null)
            {
                economics.AppendLine($"| Veterná turbína | {calc.WindSystem.InstallationCost:F0} € | " +
                    $"{calc.WindSystem.YearlySavings:F0} € | {calc.WindSystem.PaybackYears:F1} r | " +
                    $"{calc.WindSystem.ROI:F0}% |");
            }

            if (calc.HeatPumpSystem != null)
            {
                economics.AppendLine($"| Tepelné čerpadlo | {calc.HeatPumpSystem.InstallationCost:F0} € | " +
                    $"{calc.HeatPumpSystem.YearlySavings:F0} € | {calc.HeatPumpSystem.PaybackYears:F1} r | " +
                    $"{calc.HeatPumpSystem.ROI:F0}% |");
            }

            if (calc.CombinedSystem != null)
            {
                economics.AppendLine($"| **Kombinácia** | **{calc.CombinedSystem.InstallationCost:F0} €** | " +
                    $"**{calc.CombinedSystem.YearlySavings:F0} €** | **{calc.CombinedSystem.PaybackYears:F1} r** | " +
                    $"**{calc.CombinedSystem.ROI:F0}%** |");
            }

            economics.AppendLine("\n### 💶 Financovanie\n");
            economics.AppendLine("- **Dotácie:** Až do 50% nákladov (Zelená domácnostiam)");
            economics.AppendLine("- **Úver:** Zvýhodnené zelené úvery od 2.9% p.a.");
            economics.AppendLine("- **Lízing:** Možnosť lízingu s nulovým navýšením");

            return economics.ToString();
        }

        private string GenerateImplementationPlan(Session session)
        {
            var plan = new StringBuilder();

            plan.AppendLine("### Krok za krokom k úspore:\n");
            plan.AppendLine("1. **Týždeň 1-2:** Konzultácia a detailný projekt");
            plan.AppendLine("   - Obhliadka objektu");
            plan.AppendLine("   - Presné merania a výpočty");
            plan.AppendLine("   - Finálna ponuka");
            plan.AppendLine();
            plan.AppendLine("2. **Týždeň 3-4:** Administratíva");
            plan.AppendLine("   - Žiadosť o dotáciu");
            plan.AppendLine("   - Povolenia a súhlasy");
            plan.AppendLine("   - Objednávka komponentov");
            plan.AppendLine();
            plan.AppendLine("3. **Mesiac 2:** Inštalácia");
            plan.AppendLine("   - Montáž systému (2-5 dní)");
            plan.AppendLine("   - Pripojenie k sieti");
            plan.AppendLine("   - Testovanie a spustenie");
            plan.AppendLine();
            plan.AppendLine("4. **Mesiac 3+:** Monitoring");
            plan.AppendLine("   - Sledovanie výkonu");
            plan.AppendLine("   - Optimalizácia");
            plan.AppendLine("   - Servisná podpora");

            return plan.ToString();
        }

        private async Task<string> GenerateConclusion(Session session)
        {
            var bestSystem = GetBestSystem(session.Calculations);
            
            var prompt = $@"
            Vytvor krátky, presvedčivý záver (3-4 vety) pre klienta.
            Lokalita: {session.UserData.Location.Address}
            Typ budovy: {session.UserData.Building.BuildingType}
            Najlepšie riešenie: {bestSystem?.Technology}
            Úspora: {bestSystem?.YearlySavings:F0} €/rok
            Návratnosť: {bestSystem?.PaybackYears:F1} rokov
            
            Buď pozitívny a motivujúci. Zdôrazni ekologický aj ekonomický prínos.";

            var conclusion = await _openAI.GetCompletion(prompt);

            // Fallback ak OpenAI nefunguje
            if (string.IsNullOrEmpty(conclusion))
            {
                conclusion = $@"
Pre vašu nehnuteľnosť v lokalite {session.UserData.Location.Address} sme identifikovali 
výborný potenciál pre úsporu energií. {bestSystem?.Technology} vám prinesie ročnú úsporu 
{bestSystem?.YearlySavings:F0} € s návratnosťou investície za {bestSystem?.PaybackYears:F1} rokov. 
Okrem ekonomického prínosu výrazne znížite svoju uhlíkovú stopu a prispejete k ochrane životného prostredia. 
**Začnite šetriť už dnes!**";
            }

            return conclusion;
        }

        private SystemCalculation GetBestSingleSystem(CalculationResults calculations)
        {
            var systems = new List<SystemCalculation>();
            
            if (calculations.SolarSystem != null) systems.Add(calculations.SolarSystem);
            if (calculations.WindSystem != null) systems.Add(calculations.WindSystem);
            if (calculations.HeatPumpSystem != null) systems.Add(calculations.HeatPumpSystem);

            return systems.OrderBy(s => s.PaybackYears).FirstOrDefault();
        }

        private SystemCalculation GetBestSystem(CalculationResults calculations)
        {
            // Preferuj kombinovaný systém ak má dobrú návratnosť
            if (calculations.CombinedSystem != null && calculations.CombinedSystem.PaybackYears < 10)
                return calculations.CombinedSystem;

            return GetBestSingleSystem(calculations);
        }

        private string GetBuildingTypeText(string type)
        {
            return type switch
            {
                "family_house" => "Rodinný dom",
                "apartment" => "Byt",
                "company" => "Firemná budova",
                _ => "Nehnuteľnosť"
            };
        }
    }