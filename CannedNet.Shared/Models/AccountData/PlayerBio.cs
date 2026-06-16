using System.Text.Json.Serialization;

namespace CannedNet;

public class PlayerBio
{
    [JsonPropertyName("accountId")]
    public int accountId;
    [JsonPropertyName("bio")]
    public string bio;
}