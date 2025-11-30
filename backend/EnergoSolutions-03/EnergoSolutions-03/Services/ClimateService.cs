using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.DTO;
using EnergoSolutions_03.Models;
using System.Globalization;
using EnergoSolutions_03.DTO.Climate;
using EnergoSolutions_03.DTO.OpenMeteo;

namespace EnergoSolutions_03.Services;

public class ClimateService : IClimateService
{
    private readonly HttpClient _http;

    public ClimateService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ClimateResponseDto?> GetClimateHeatingAsync(ClimateRequestDto dto)
    {
        DateTime today = DateTime.Today;
        DateTime startDate = new(today.Year - 5, 1, 1);
        DateTime endDate = today;

        string url =
            $"v1/archive?latitude={dto.Lat.ToString(CultureInfo.InvariantCulture)}" +
            $"&longitude={dto.Lon.ToString(CultureInfo.InvariantCulture)}" +
            $"&start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}" +
            $"&hourly=temperature_2m&timezone=auto";

        HttpResponseMessage httpResponse = await _http.GetAsync(url);
        if (!httpResponse.IsSuccessStatusCode)
            return null;

        OpenMeteoArchiveClimateDto? payload =
            await httpResponse.Content.ReadFromJsonAsync<OpenMeteoArchiveClimateDto>();

        if (payload is null ||
            payload.Hourly.Time.Count != payload.Hourly.Temperature_2m.Count)
            return null;

        var bins = new List<(string Label, double? Low, double? High)>
        {
            ("<= -15", null, -15),
            ("-15 až -10", -15, -10),
            ("-10 až -5", -10, -5),
            ("-5 až 0", -5, 0),
            ("0 až +5", 0, 5),
            ("+5 až +10", 5, 10),
            ("+10 až +15", 10, 15),
            ("> +15", 15, null),
        };

        string Classify(double t)
        {
            foreach (var (label, low, high) in bins)
            {
                if (low is null && t <= high) return label;
                if (high is null && t > low) return label;
                if (low is not null && high is not null && low <= t && t < high) return label;
            }
            return "unknown";
        }

        var yearlyStats = new Dictionary<int, YearStats>();
        var dailyTemps = new Dictionary<DateTime, List<double>>();

        foreach (var pair in payload.Hourly.Time.Zip(payload.Hourly.Temperature_2m,
                     (timeString, temperature) => new { timeString, temperature }))
        {
            DateTime dt = DateTime.Parse(pair.timeString, null, DateTimeStyles.AssumeUniversal);
            double temp = pair.temperature;

            if (!yearlyStats.ContainsKey(dt.Year))
            {
                // initialize bin counts and ensure an "unknown" bin exists to avoid KeyNotFoundException
                var counts = bins.ToDictionary(b => b.Label, _ => 0);
                if (!counts.ContainsKey("unknown")) counts["unknown"] = 0;

                yearlyStats[dt.Year] = new YearStats
                {
                    BinCounts = counts
                };
            }

            YearStats stats = yearlyStats[dt.Year];
            string binLabel = Classify(temp);

            // guard against unexpected labels (e.g. "unknown")
            if (!stats.BinCounts.ContainsKey(binLabel))
                stats.BinCounts[binLabel] = 0;

            stats.BinCounts[binLabel]++;
            stats.TotalHours++;

            if (stats.MinTemp is null || temp < stats.MinTemp)
                stats.MinTemp = temp;

            if (temp < -10)
                stats.HoursBelow10++;

            if (temp < -15)
                stats.HoursBelow15++;

            if (!dailyTemps.ContainsKey(dt.Date))
                dailyTemps[dt.Date] = new List<double>();

            dailyTemps[dt.Date].Add(temp);
        }

        foreach (var dayEntry in dailyTemps)
        {
            double average = dayEntry.Value.Average();
            if (average < 20)
            {
                int year = dayEntry.Key.Year;
                yearlyStats[year].Hdd20 += 20 - average;
            }
        }

        var multiYearBins = bins.ToDictionary(b => b.Label, _ => 0);
        // ensure unknown key exists in multiYearBins too
        if (!multiYearBins.ContainsKey("unknown")) multiYearBins["unknown"] = 0;
        int multiYearTotalHours = 0;
        var yearOutput = new List<ClimateYearDto>();

        foreach (int year in yearlyStats.Keys.OrderBy(y => y))
        {
            YearStats stats = yearlyStats[year];
            multiYearTotalHours += stats.TotalHours;

            foreach (var (label, _, _) in bins)
                multiYearBins[label] += stats.BinCounts[label];

            var binList = new List<TempBinDto>();
            foreach (var (label, _, _) in bins)
            {
                int hours = stats.BinCounts[label];
                double percent = stats.TotalHours == 0
                    ? 0
                    : (double)hours / stats.TotalHours * 100;

                binList.Add(new TempBinDto
                {
                    Range = label,
                    Hours = hours,
                    PercentOfYear = percent
                });
            }

            yearOutput.Add(new ClimateYearDto
            {
                Year = year,
                TempBins = binList,
                Hdd20 = stats.Hdd20,
                MinTemp = stats.MinTemp ?? 0,
                HoursBelowMinus10 = stats.HoursBelow10,
                HoursBelowMinus15 = stats.HoursBelow15,
                TotalHours = stats.TotalHours
            });
        }

        var multiYearOutput = new List<MultiYearTempBinDto>();
        foreach (var (label, _, _) in bins)
        {
            double percent = multiYearTotalHours == 0
                ? 0
                : (double)multiYearBins[label] / multiYearTotalHours * 100;

            multiYearOutput.Add(new MultiYearTempBinDto
            {
                Range = label,
                AvgPercentOfYear = percent
            });
        }

        return new ClimateResponseDto
        {
            Location = new LocationDto
            {
                Lat = dto.Lat,
                Lon = dto.Lon
            },
            Period = new PeriodDto
            {
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd")
            },
            Years = yearOutput,
            MultiYear = new ClimateMultiYearDto
            {
                TempBinsAvgPercent = multiYearOutput,
                TotalYears = yearlyStats.Count
            }
        };
    }
}