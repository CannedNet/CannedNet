using System.Text.Json.Serialization;

namespace CannedNet;

public class SubscriptionList
{
    [JsonPropertyName("playerIds")]
    public List<int>? PlayerIds { get; set; }
}
