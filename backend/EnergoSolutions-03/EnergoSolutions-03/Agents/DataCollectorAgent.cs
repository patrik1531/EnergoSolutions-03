using System.Text.Json;
using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.Models.Agent;

namespace EnergoSolutions_03.Agents;

public class DataCollectorAgent : IDataCollectorAgent
{
    private readonly IOpenAIService _openAI;
    private readonly IWeatherApiService _weatherApi;

    public DataCollectorAgent(IOpenAIService openAI, IWeatherApiService weatherApi)
    {
        _openAI = openAI;
        _weatherApi = weatherApi;
    }

    public async Task<AgentResponse> ProcessMessage(Session session, string message)
    {
        if (session == null)
        {
            return new AgentResponse
            {
                Message = "Internal error: session is null.",
                IsComplete = false,
                Progress = 0
            };
        }

        EnsureUserDataInitialized(session);

        // Prvá správa – uvítanie
        if (string.IsNullOrWhiteSpace(message))
        {
            return new AgentResponse
            {
                Message = "Dobrý deň! Som váš energetický poradca. Pomôžem vám nájsť ideálne riešenie pre úsporu energií. 🌱\n\n" +
                         "Začnime základnými informáciami. V akej obci alebo meste sa nachádza vaša nehnuteľnosť?",
                IsComplete = false,
                Progress = 10
            };
        }

        // 1) extrahuj údaje z aktuálnej správy pomocou AI
        var extracted = await ExtractInformation(message, session.UserData);

        // Ak AI vyhodnotilo odpoveď ako nerelevantnú
        if (extracted.TryGetValue("irrelevant", out var irrObj) &&
            irrObj is bool irrBool &&
            irrBool)
        {
            return new AgentResponse
            {
                Message = "Zdá sa, že vaša odpoveď nesúvisela s otázkou o vašej nehnuteľnosti. Skúste prosím odpovedať konkrétnejšie. 🙂",
                IsComplete = false,
                Progress = CalculateCollectionProgress(session.UserData)
            };
        }

        // 2) zapíš extrahované údaje do session.UserData
        UpdateUserData(session.UserData, extracted);

        // 3) zisti, čo ešte chýba
        var missingFields = GetMissingRequiredFields(session.UserData);

        if (missingFields.Count > 0)
        {
            var nextQuestion = GenerateQuestion(missingFields[0], session.UserData);

            return new AgentResponse
            {
                Message = nextQuestion,
                IsComplete = false,
                Progress = CalculateCollectionProgress(session.UserData)
            };
        }

        // 4) máme všetky údaje – načítame technické dáta
        await FetchTechnicalData(session);

        return new AgentResponse
        {
            Message = "Výborne, mám všetky potrebné informácie! 📊\n" +
                      "Teraz analyzujem klimatické podmienky vašej lokality a technické možnosti...",
            IsComplete = true,
            Progress = 25
        };
    }

    // ============================================================
    //  EXTRAKCIA CEZ OPENAI
    // ============================================================

    private async Task<Dictionary<string, object>> ExtractInformation(string message, UserData currentData)
{
    var prompt = $"""
You are an information extraction assistant for an energy consulting chatbot.

USER MESSAGE:
{message}

CURRENT KNOWN DATA (JSON):
{JsonSerializer.Serialize(currentData)}

Your task:
- Analyze the user's message.
- Extract any of the following fields if present.
- If something is not mentioned, omit that field completely.

Fields:
- address: string (any place, street, village, town, city, postal code)
- buildingType: string (one of: "family_house", "apartment", "company")
- heatedAreaM2: number (heated floor area in m²)
- insulationLevel: string (one of: "poor", "average", "good", "excellent")
- electricityKwhYear: number (yearly electricity consumption in kWh)
- heatingFuel: string (e.g. "gas", "electricity", "wood", "heat_pump")
- roofAreaM2: number (usable roof area in m²)
- phase: string ("1f" or "3f")

If the user's message is irrelevant to the house / building / energy topic, respond with:
{"irrelevant":true}

VERY IMPORTANT:
- Respond with ONE valid JSON object only.
- No extra text, no explanations, no code fences.
- If you only know some fields, include only those fields in the JSON.
""";

    var response = await _openAI.GetCompletion(prompt);

    if (string.IsNullOrWhiteSpace(response) ||
        response.StartsWith("AI API error", StringComparison.OrdinalIgnoreCase) ||
        response.StartsWith("AI network error", StringComparison.OrdinalIgnoreCase) ||
        response.StartsWith("AI parsing error", StringComparison.OrdinalIgnoreCase))
    {
        return new Dictionary<string, object>();
    }

    Console.WriteLine("=== OpenAI raw ===");
    Console.WriteLine(response);
    Console.WriteLine("==================");

    try
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            response,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (raw == null)
        {
            return new Dictionary<string, object>();
        }

        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in raw)
        {
            Console.WriteLine($"ENTRY: key='{entry.Key}', kind={entry.Value.ValueKind}, raw='{entry.Value.GetRawText()}'");

            object? typedValue = null;

            switch (entry.Value.ValueKind)
            {
                case JsonValueKind.String:
                    typedValue = entry.Value.GetString();
                    break;
                case JsonValueKind.Number:
                    if (entry.Value.TryGetInt32(out var i))
                    {
                        typedValue = i;
                    }
                    else if (entry.Value.TryGetDouble(out var d))
                    {
                        typedValue = d;
                    }
                    break;
                case JsonValueKind.True:
                    typedValue = true;
                    break;
                case JsonValueKind.False:
                    typedValue = false;
                    break;
            }

            if (typedValue != null)
            {
                // žiadna mágia – použijeme presne ten istý kľúč, čo prišiel z AI
                result[entry.Key] = typedValue;
            }
        }

        Console.WriteLine("=== Extracted keys ===");
        foreach (var kv in result)
        {
            Console.WriteLine($"  {kv.Key} = {kv.Value}");
        }
        Console.WriteLine("======================");

        return result;
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"[DataCollectorAgent] JSON parse error: {ex.Message}");
        return new Dictionary<string, object>();
    }
}

    // ============================================================
    //  UPDATE USERDATA
    // ============================================================

    private void UpdateUserData(UserData data, Dictionary<string, object> extracted)
    {
        if (data == null || extracted == null || extracted.Count == 0) return;

        if (extracted.TryGetValue("address", out var addrObj) && addrObj != null)
        {
            EnsureLocationInitialized(data);
            data.Location.Address = addrObj.ToString();
        }

        if (extracted.TryGetValue("buildingType", out var btObj) && btObj != null)
        {
            EnsureBuildingInitialized(data);
            data.Building.BuildingType = btObj.ToString();
        }

        if (extracted.TryGetValue("heatedAreaM2", out var areaObj) && areaObj != null)
        {
            if (int.TryParse(areaObj.ToString(), out var area))
            {
                EnsureBuildingInitialized(data);
                data.Building.HeatedAreaM2 = area;
            }
        }

        if (extracted.TryGetValue("insulationLevel", out var insObj) && insObj != null)
        {
            EnsureBuildingInitialized(data);
            data.Building.InsulationLevel = insObj.ToString();
        }

        if (extracted.TryGetValue("electricityKwhYear", out var kwhObj) && kwhObj != null)
        {
            if (int.TryParse(kwhObj.ToString(), out var kwh))
            {
                EnsureConsumptionInitialized(data);
                data.Consumption.ElectricityKwhYear = kwh;
            }
        }

        if (extracted.TryGetValue("heatingFuel", out var hfObj) && hfObj != null)
        {
            EnsureConsumptionInitialized(data);
            data.Consumption.HeatingFuel = hfObj.ToString();
        }

        if (extracted.TryGetValue("roofAreaM2", out var roofObj) && roofObj != null)
        {
            if (int.TryParse(roofObj.ToString(), out var roofArea))
            {
                EnsureRoofInitialized(data);
                data.Roof.RoofAreaM2 = roofArea;
            }
        }

        if (extracted.TryGetValue("phase", out var phaseObj) && phaseObj != null)
        {
            EnsureElectricalInitialized(data);
            data.Electrical.Phase = phaseObj.ToString();
        }
    }

    // ============================================================
    //  MISSING FIELDS + QUESTIONS
    // ============================================================

    private List<string> GetMissingRequiredFields(UserData data)
    {
        var missing = new List<string>();

        if (string.IsNullOrEmpty(data?.Location?.Address))
            missing.Add("address");
        if (string.IsNullOrEmpty(data?.Building?.BuildingType))
            missing.Add("buildingType");
        if (data?.Building?.HeatedAreaM2 == null)
            missing.Add("heatedAreaM2");
        if (data?.Consumption?.ElectricityKwhYear == null)
            missing.Add("electricityKwhYear");
        if (string.IsNullOrEmpty(data?.Consumption?.HeatingFuel))
            missing.Add("heatingFuel");

        if (data?.Building?.BuildingType == "family_house")
        {
            if (data?.Roof?.RoofAreaM2 == null)
                missing.Add("roofAreaM2");
        }

        return missing;
    }

    private string GenerateQuestion(string missingField, UserData currentData)
    {
        return missingField switch
        {
            "address" => "V ktorej obci alebo meste sa nachádza vaša nehnuteľnosť?",
            "buildingType" => "Ide o rodinný dom, byt alebo firemnú budovu?",
            "heatedAreaM2" => "Aká je vykurovaná plocha vašej nehnuteľnosti v m²?",
            "electricityKwhYear" => "Koľko kWh elektriny spotrebujete ročne? (nájdete na vyúčtovaní)",
            "heatingFuel" => "Čím kúrite? (plyn, elektrina, drevo, tepelné čerpadlo...)",
            "roofAreaM2" => "Aká je približná využiteľná plocha vašej strechy v m²?",
            _ => "Máte ešte nejaké doplňujúce informácie o vašej nehnuteľnosti?"
        };
    }

    // ============================================================
    //  TECHNICKÉ DÁTA
    // ============================================================

    private async Task FetchTechnicalData(Session session)
    {
        var location = session?.UserData?.Location?.Address;
        if (string.IsNullOrWhiteSpace(location)) return;

        var coords = await _weatherApi.GetCoordinates(location);
        var technicalData = await _weatherApi.GetSummaryData(coords.Lat, coords.Lon);

        session.TechnicalData = technicalData;
    }

    private int CalculateCollectionProgress(UserData data)
    {
        var filled = 0;
        const int total = 5;

        if (!string.IsNullOrEmpty(data?.Location?.Address)) filled++;
        if (!string.IsNullOrEmpty(data?.Building?.BuildingType)) filled++;
        if (data?.Building?.HeatedAreaM2 != null) filled++;
        if (data?.Consumption?.ElectricityKwhYear != null) filled++;
        if (!string.IsNullOrEmpty(data?.Consumption?.HeatingFuel)) filled++;

        return (filled * 25) / total;
    }

    // ============================================================
    //  INITIALIZATION HELPERS
    // ============================================================

    private void EnsureUserDataInitialized(Session session)
    {
        if (session.UserData == null) session.UserData = new UserData();
        EnsureLocationInitialized(session.UserData);
        EnsureBuildingInitialized(session.UserData);
        EnsureConsumptionInitialized(session.UserData);
        EnsureRoofInitialized(session.UserData);
        EnsureElectricalInitialized(session.UserData);
    }

    private void EnsureLocationInitialized(UserData data)
    {
        if (data.Location == null) data.Location = new Location();
    }

    private void EnsureBuildingInitialized(UserData data)
    {
        if (data.Building == null) data.Building = new Building();
    }

    private void EnsureConsumptionInitialized(UserData data)
    {
        if (data.Consumption == null) data.Consumption = new Consumption();
    }

    private void EnsureRoofInitialized(UserData data)
    {
        if (data.Roof == null) data.Roof = new Roof();
    }

    private void EnsureElectricalInitialized(UserData data)
    {
        if (data.Electrical == null) data.Electrical = new Electrical();
    }
}