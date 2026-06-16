using System.Text.Json.Serialization;

namespace CannedNet;

public class PlayerProgression
{
    public int Id { get; set; }
    [JsonPropertyName("playerId")]
    public int PlayerId { get; set; }
    [JsonPropertyName("level")]
    public int Level { get; set; }
    [JsonPropertyName("xp")]
    public int Xp { get; set; }
}

public class PlayerProgressionBulkResponse
{
    [JsonPropertyName("playerId")]
    public int PlayerId { get; set; }
    [JsonPropertyName("level")]
    public int Level { get; set; }
    [JsonPropertyName("xp")]
    public int Xp { get; set; }
}
