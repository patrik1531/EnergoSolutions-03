namespace EnergoSolutions_03.Models;

public class WindYearStats
{
    public Dictionary<string, int> BinCounts { get; set; } = new();
    public int TotalHours { get; set; }
    public double SumSpeed { get; set; }
    public int HoursAbove3 { get; set; }
    public int HoursAbove6 { get; set; }
}