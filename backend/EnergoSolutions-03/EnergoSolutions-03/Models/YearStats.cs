namespace EnergoSolutions_03.Models;

public class YearStats
{
    public Dictionary<string, int> BinCounts { get; set; }
    public int TotalHours { get; set; }
    public double? MinTemp { get; set; }
    public int HoursBelow10 { get; set; }
    public int HoursBelow15 { get; set; }
    public double Hdd20 { get; set; }
}