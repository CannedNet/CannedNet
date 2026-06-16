using System.Text.Json.Serialization;

namespace CannedNet;

public class RoomStats
{
    [JsonPropertyName("CheerCount")]
    public int CheerCount { get; set; }
    [JsonPropertyName("FavoriteCount")]
    public int FavoriteCount { get; set; }
    [JsonPropertyName("VisitorCount")]
    public int VisitorCount { get; set; }
    [JsonPropertyName("VisitCount")]
    public int VisitCount { get; set; }
}