using System.Text.Json.Serialization;

namespace CannedNet;

public class PlayerSetting
{
    public int Id { get; set; }
    [JsonPropertyName("playerId")]
    public int PlayerId { get; set; }
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";
    [JsonPropertyName("value")]
    public string Value { get; set; } = "";
}

