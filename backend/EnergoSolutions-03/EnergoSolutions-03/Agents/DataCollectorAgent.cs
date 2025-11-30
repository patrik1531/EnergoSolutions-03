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
        Extrahuj informácie z tejto správy: '{message}'

        Aktuálne údaje: {JsonSerializer.Serialize(currentData)}

        Hľadaj:
        - address (mesto/obec)
        - building_type (rodinný dom='family_house', byt='apartment', firma='company')
        - heated_area_m2 (vykurovaná plocha v m²)
        - insulation_level (zlá='poor', priemerná='average', dobrá='good')
        - electricity_kwh_year (ročná spotreba elektriny v kWh)
        - heating_fuel (plyn='gas', elektrina='electricity', drevo='wood')
        - roof_area_m2 (plocha strechy v m²)
        - orientations (orientácia: juh='south', východ='east', západ='west', sever='north')
        - phase (1f alebo 3f)

        Respond with a single valid JSON object only (no extra text, no code fences). The JSON object keys should be exactly the names above and missing keys can be omitted.
        ";

            var response = await _openAI.GetCompletion(basePrompt);

            // Try to extract JSON object from the response (handles model prefix/suffix text)
            if (TryExtractJson(response, out var json))
            {
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
                    return dict ?? new Dictionary<string, object>();
                }
                catch (JsonException)
                {
                    // fallthrough to return empty dict on parse error
                }
            }

            // If extraction/deserialization failed, return empty dict (agent will ask follow-ups)
            return new Dictionary<string, object>();
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
            if (extracted.ContainsKey("address"))
                data.Location.Address = extracted["address"].ToString();
            
            if (extracted.ContainsKey("building_type"))
                data.Building.BuildingType = extracted["building_type"].ToString();
            
            if (extracted.ContainsKey("heated_area_m2") && int.TryParse(extracted["heated_area_m2"].ToString(), out int area))
                data.Building.HeatedAreaM2 = area;
            
            if (extracted.ContainsKey("insulation_level"))
                data.Building.InsulationLevel = extracted["insulation_level"].ToString();
            
            if (extracted.ContainsKey("electricity_kwh_year") && int.TryParse(extracted["electricity_kwh_year"].ToString(), out int kwh))
                data.Consumption.ElectricityKwhYear = kwh;
            
            if (extracted.ContainsKey("heating_fuel"))
                data.Consumption.HeatingFuel = extracted["heating_fuel"].ToString();
            
            if (extracted.ContainsKey("roof_area_m2") && int.TryParse(extracted["roof_area_m2"].ToString(), out int roofArea))
                data.Roof.RoofAreaM2 = roofArea;
            
            if (extracted.ContainsKey("phase"))
                data.Electrical.Phase = extracted["phase"].ToString();
        }

        private List<string> GetMissingRequiredFields(UserData data)
        {
            var missing = new List<string>();

            if (string.IsNullOrEmpty(data.Location.Address))
                missing.Add("address");
            if (string.IsNullOrEmpty(data.Building.BuildingType))
                missing.Add("building_type");
            if (data.Building.HeatedAreaM2 == null)
                missing.Add("heated_area");
            if (data.Consumption.ElectricityKwhYear == null)
                missing.Add("electricity_consumption");
            if (string.IsNullOrEmpty(data.Consumption.HeatingFuel))
                missing.Add("heating_fuel");
            
            // Pre rodinný dom potrebujeme info o streche
            if (data.Building.BuildingType == "family_house")
            {
                if (data.Roof.RoofAreaM2 == null)
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
            // Zavolaj existujúce API pre technické dáta
            var location = session.UserData.Location.Address;
            
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

            if (!string.IsNullOrEmpty(data.Location.Address)) filled++;
            if (!string.IsNullOrEmpty(data.Building.BuildingType)) filled++;
            if (data.Building.HeatedAreaM2 != null) filled++;
            if (data.Consumption.ElectricityKwhYear != null) filled++;
            if (!string.IsNullOrEmpty(data.Consumption.HeatingFuel)) filled++;

            return (filled * 25) / total; // 0-25% progress
        }
    }