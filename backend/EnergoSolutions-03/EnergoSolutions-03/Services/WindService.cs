using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.DTO.Wind;
using EnergoSolutions_03.DTO.OpenMeteo;
using EnergoSolutions_03.Models;
using System.Globalization;

namespace EnergoSolutions_03.Services;

public class WindService : IWindService
{
    private readonly HttpClient _http;

    public WindService(HttpClient http)
    {
        _http = http;
    }

    public async Task<WindResponseDto?> GetWindStatsAsync(WindRequestDto dto)
    {
        DateTime today = DateTime.Today;
        DateTime startDate = new(today.Year - 5, 1, 1);
        DateTime endDate = today;

        string url =
            $"v1/archive?latitude={dto.Lat.ToString(CultureInfo.InvariantCulture)}" +
            $"&longitude={dto.Lon.ToString(CultureInfo.InvariantCulture)}" +
            $"&start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}" +
            $"&hourly=windspeed_10m&timezone=auto";

        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        var payload = await response.Content.ReadFromJsonAsync<OpenMeteoArchiveWindDto>();
        if (payload is null ||
            payload.Hourly.Time.Count != payload.Hourly.WindSpeed_10m.Count)
            return null;

        var bins = new List<(string Label, double? Low, double? High)>
        {
            ("<= 3 m/s", null, 3),
            ("3 - 6 m/s", 3, 6),
            ("6 - 9 m/s", 6, 9),
            ("9 - 12 m/s", 9, 12),
            ("> 12 m/s", 12, null),
        };

        string Classify(double v)
        {
            foreach (var (label, low, high) in bins)
            {
                if (low is null && v <= high) return label;
                if (high is null && v > low) return label;
                if (low is not null && high is not null && low <= v && v < high) return label;
            }
            return "unknown";
        }

        var yearly = new Dictionary<int, WindYearStats>();

        foreach (var pair in payload.Hourly.Time.Zip(payload.Hourly.WindSpeed_10m,
                     (t, s) => new { t, s }))
        {
            DateTime dt = DateTime.Parse(pair.t, null, DateTimeStyles.AssumeUniversal);
            double speed = pair.s;

            if (!yearly.ContainsKey(dt.Year))
            {
                var counts = bins.ToDictionary(b => b.Label, _ => 0);
                // ensure 'unknown' exists so unexpected classifications won't crash
                if (!counts.ContainsKey("unknown")) counts["unknown"] = 0;

                yearly[dt.Year] = new WindYearStats
                {
                    BinCounts = counts
                };
            }

            var y = yearly[dt.Year];
            y.TotalHours++;
            y.SumSpeed += speed;

            string bin = Classify(speed);
            if (!y.BinCounts.ContainsKey(bin))
                y.BinCounts[bin] = 0; // guard against unexpected label
            y.BinCounts[bin]++;

            if (speed > 3) y.HoursAbove3++;
            if (speed > 6) y.HoursAbove6++;
        }

        var multiBins = bins.ToDictionary(b => b.Label, _ => 0);
        if (!multiBins.ContainsKey("unknown")) multiBins["unknown"] = 0;
        int multiTotalHours = 0;
        double multiSumSpeed = 0;

        var yearOutput = new List<WindYearDto>();

        foreach (int year in yearly.Keys.OrderBy(y => y))
        {
            var st = yearly[year];
            multiTotalHours += st.TotalHours;
            multiSumSpeed += st.SumSpeed;

            foreach (var (label, _, _) in bins)
            {
                // guard access in case some labels are missing
                if (!st.BinCounts.ContainsKey(label)) st.BinCounts[label] = 0;
                multiBins[label] += st.BinCounts[label];
            }

            var binList = new List<WindBinDto>();
            foreach (var (label, _, _) in bins)
            {
                int hours = st.BinCounts.ContainsKey(label) ? st.BinCounts[label] : 0;
                double percent = st.TotalHours == 0 ? 0 : (double)hours / st.TotalHours * 100;

                binList.Add(new WindBinDto
                {
                    Range = label,
                    Hours = hours,
                    PercentOfYear = percent
                });
            }

            double meanSpeed = st.TotalHours == 0 ? 0 : st.SumSpeed / st.TotalHours;

            yearOutput.Add(new WindYearDto
            {
                Year = year,
                WindBins = binList,
                MeanSpeed = meanSpeed,
                HoursAbove3 = st.HoursAbove3,
                HoursAbove6 = st.HoursAbove6,
                TotalHours = st.TotalHours
            });
        }

        var multiOut = new List<MultiYearWindBinDto>();
        foreach (var (label, _, _) in bins)
        {
            double percent = multiTotalHours == 0 ? 0 : (double)multiBins[label] / multiTotalHours * 100;

            multiOut.Add(new MultiYearWindBinDto
            {
                Range = label,
                AvgPercentOfYear = percent
            });
        }

        double? overallMean = multiTotalHours == 0
            ? null
            : multiSumSpeed / multiTotalHours;

        return new WindResponseDto
        {
            Location = new WindLocationDto { Lat = dto.Lat, Lon = dto.Lon },
            Period = new WindPeriodDto
            {
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd")
            },
            Years = yearOutput,
            MultiYear = new WindMultiYearDto
            {
                WindBinsAvgPercent = multiOut,
                OverallMeanSpeed = overallMean,
                TotalYears = yearly.Count
            }
        };
    }
}