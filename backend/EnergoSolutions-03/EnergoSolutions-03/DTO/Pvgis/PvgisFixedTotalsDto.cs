using System.Text.Json.Serialization;

namespace EnergoSolutions_03.DTO.Pvgis;

public class PvgisFixedTotalsDto
{
    [JsonPropertyName("E_y")]
    public double? E_y { get; set; }
}