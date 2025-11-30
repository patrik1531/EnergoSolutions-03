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

            // Ensure user data and nested objects exist before any updates
            EnsureUserDataInitialized(session);

            // Pri prvej správe
            if (string.IsNullOrEmpty(message))
            {
                return new AgentResponse
                {
                    Message = "Dobrý deň! Som váš energetický poradca. Pomôžem vám nájsť ideálne riešenie pre úsporu energií. 🌱\n\n" +
                             "Začnime základnými informáciami. V akej obci alebo meste sa nachádza váša nehnuteľnosť?",
                    IsComplete = false,
                    Progress = 10
                };
            }

            // Extrahuj informácie zo správy
            var extracted = await ExtractInformation(message, session.UserData);
            UpdateUserData(session.UserData, extracted);

            // Skontroluj čo chýba
            var missingFields = GetMissingRequiredFields(session.UserData);

            if (missingFields.Count > 0)
            {
                // Generuj otázku pre chýbajúce pole
                var nextQuestion = GenerateQuestion(missingFields[0], session.UserData);

                return new AgentResponse
                {
                    Message = nextQuestion,
                    IsComplete = false,
                    Progress = CalculateCollectionProgress(session.UserData)
                };
            }

            // Máme všetky údaje, získaj technické dáta
            await FetchTechnicalData(session);

            return new AgentResponse
            {
                Message = "Výborne, mám všetky potrebné informácie! 📊\n" +
                         "Teraz analyzujem klimatické podmienky vašej lokality a technické možnosti...",
                IsComplete = true,
                Progress = 25
            };
        }

        private async Task<Dictionary<string, object>> ExtractInformation(string message, UserData currentData)
        {
            var basePrompt = $@"
        You are an information extraction AI. Your job is to analyze the user's message and assign any meaningful values to the correct fields. 

        Be flexible: the user may answer with an address, town name, building size, heating type, energy consumption, roof details, or random combinations of these.

        If the message contains ANY of the following types of information, extract them:

        - address → any place, street, city, village, town, postal code
        - building_type → rodinný dom, byt, firma (map to: family_house, apartment, company)
        - heated_area_m2 → any number followed by m², m2, alebo bez jednotky if logically area
        - insulation_level → žiadne/poor, dobré/good, perfektné/excellent
        - electricity_kwh_year → any number clearly referencing yearly energy consumption
        - heating_fuel → plyn/gas, elektrina/electricity, drevo/wood, TČ/heatpump
        - roof_area_m2 → numbers relating to roof size
        - orientations → juh/south, východ/east, západ/west, sever/north
        - phase → '1f', '3f', 'jednofázová', 'trojfázová'

        If the user's message is irrelevant, unclear, or unrelated, return:
        {{ ""irrelevant"": true }}

        Current known data: {JsonSerializer.Serialize(currentData)}

        Return strictly one JSON object with only found keys. No explanation text.
        ";

            var response = await _openAI.GetCompletion(basePrompt);

            // basic guard na chyby z OpenAIService
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
                // najprv ako Dictionary<string, JsonElement>
                var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                    response,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (raw == null)
                {
                    return new Dictionary<string, object>();
                }

                // normovaný výstup – kľúče lowercase, bez medzier
                var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                foreach (var kv in raw)
                {
                    var key = kv.Key.Trim().ToLowerInvariant();
                    var value = kv.Value;

                    // aliasy – keby model vrátil iné názvy
                    key = key switch
                    {
                        "city" or "town" or "mesto" or "obec" => "address",
                        "roof_area" => "roof_area_m2",
                        "electricity_consumption" => "electricity_kwh_year",
                        _ => key
                    };

                    object? typed = null;

                    switch (value.ValueKind)
                    {
                        case JsonValueKind.String:
                            typed = value.GetString();
                            break;
                        case JsonValueKind.Number:
                            if (value.TryGetInt32(out var i))
                                typed = i;
                            else if (value.TryGetDouble(out var d))
                                typed = d;
                            break;
                        case JsonValueKind.True:
                            typed = true;
                            break;
                        case JsonValueKind.False:
                            typed = false;
                            break;
                    }

                    if (typed != null)
                    {
                        result[key] = typed;
                    }
                }

                return result;
            }
            catch (JsonException)
            {
                // fallback – ak by AI predsa len poslalo text + JSON
                if (TryExtractJson(response, out var json))
                {
                    try
                    {
                        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            json,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        // aj tu môžeme spraviť jednoduchú normalizáciu
                        return dict?
                                   .ToDictionary(
                                       kv => kv.Key.Trim().ToLowerInvariant(),
                                       kv => kv.Value,
                                       StringComparer.OrdinalIgnoreCase)
                               ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        // ignore
                    }
                }

                return new Dictionary<string, object>();
            }
        }

        private bool TryExtractJson(string input, out string jsonOut)
        {
            jsonOut = null;
            if (string.IsNullOrWhiteSpace(input)) return false;

            // Find first '{'
            int start = input.IndexOf('{');
            if (start < 0) return false;

            int depth = 0;
            for (int i = start; i < input.Length; i++)
            {
                if (input[i] == '{') depth++;
                else if (input[i] == '}') depth--;

                if (depth == 0)
                {
                    int end = i;
                    var candidate = input.Substring(start, end - start + 1).Trim();

                    // Quick validate by attempting to parse
                    try
                    {
                        using var doc = JsonDocument.Parse(candidate);
                        jsonOut = candidate;
                        return true;
                    }
                    catch (JsonException)
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        private void UpdateUserData(UserData data, Dictionary<string, object> extracted)
        {
            if (data == null || extracted == null || extracted.Count == 0) return;

            if (extracted.TryGetValue("address", out var addrObj) && addrObj != null)
            {
                EnsureLocationInitialized(data);
                data.Location.Address = addrObj.ToString();
            }

            if (extracted.TryGetValue("building_type", out var btObj) && btObj != null)
            {
                EnsureBuildingInitialized(data);
                data.Building.BuildingType = btObj.ToString();
            }

            if (extracted.TryGetValue("heated_area_m2", out var areaObj) && areaObj != null)
            {
                if (int.TryParse(areaObj.ToString(), out int area))
                {
                    EnsureBuildingInitialized(data);
                    data.Building.HeatedAreaM2 = area;
                }
            }

            if (extracted.TryGetValue("insulation_level", out var insObj) && insObj != null)
            {
                EnsureBuildingInitialized(data);
                data.Building.InsulationLevel = insObj.ToString();
            }

            if (extracted.TryGetValue("electricity_kwh_year", out var kwhObj) && kwhObj != null)
            {
                if (int.TryParse(kwhObj.ToString(), out int kwh))
                {
                    EnsureConsumptionInitialized(data);
                    data.Consumption.ElectricityKwhYear = kwh;
                }
            }

            if (extracted.TryGetValue("heating_fuel", out var hfObj) && hfObj != null)
            {
                EnsureConsumptionInitialized(data);
                data.Consumption.HeatingFuel = hfObj.ToString();
            }

            if (extracted.TryGetValue("roof_area_m2", out var roofObj) && roofObj != null)
            {
                if (int.TryParse(roofObj.ToString(), out int roofArea))
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

        private List<string> GetMissingRequiredFields(UserData data)
        {
            var missing = new List<string>();

            if (string.IsNullOrEmpty(data?.Location?.Address))
                missing.Add("address");
            if (string.IsNullOrEmpty(data?.Building?.BuildingType))
                missing.Add("building_type");
            if (data?.Building?.HeatedAreaM2 == null)
                missing.Add("heated_area");
            if (data?.Consumption?.ElectricityKwhYear == null)
                missing.Add("electricity_consumption");
            if (string.IsNullOrEmpty(data?.Consumption?.HeatingFuel))
                missing.Add("heating_fuel");

            // Pre rodinný dom potrebujeme info o streche
            if (data?.Building?.BuildingType == "family_house")
            {
                if (data?.Roof?.RoofAreaM2 == null)
                    missing.Add("roof_area");
            }

            return missing;
        }

        private string GenerateQuestion(string missingField, UserData currentData)
        {
            return missingField switch
            {
                "address" => "V ktorej obci alebo meste sa nachádza vaša nehnuteľnosť?",
                "building_type" => "Ide o rodinný dom, byt alebo firemnú budovu?",
                "heated_area" => "Aká je vykurovaná plocha vašej nehnuteľnosti v m²?",
                "electricity_consumption" => "Koľko kWh elektriny spotrebujete ročne? (nájdete na vyúčtovaní)",
                "heating_fuel" => "Čím kúrite? (plyn, elektrina, drevo, tepelné čerpadlo...)",
                "roof_area" => "Aká je približná využiteľná plocha vašej strechy v m²?",
                _ => "Máte ešte nejaké doplňujúce informácie o vašej nehnuteľnosti?"
            };
        }

        private async Task FetchTechnicalData(Session session)
        {
            var location = session?.UserData?.Location?.Address;
            if (string.IsNullOrWhiteSpace(location)) return;

            // Geocoding
            var coords = await _weatherApi.GetCoordinates(location);

            // Summary data
            var technicalData = await _weatherApi.GetSummaryData(coords.Lat, coords.Lon);

            session.TechnicalData = technicalData;
        }

        private int CalculateCollectionProgress(UserData data)
        {
            int filled = 0;
            int total = 5; // Základné povinné polia

            if (!string.IsNullOrEmpty(data?.Location?.Address)) filled++;
            if (!string.IsNullOrEmpty(data?.Building?.BuildingType)) filled++;
            if (data?.Building?.HeatedAreaM2 != null) filled++;
            if (data?.Consumption?.ElectricityKwhYear != null) filled++;
            if (!string.IsNullOrEmpty(data?.Consumption?.HeatingFuel)) filled++;

            return (filled * 25) / total; // 0-25% progress
        }

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