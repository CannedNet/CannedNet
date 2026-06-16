using System.Text.Json.Serialization;

namespace CannedNet;

public class ChecklistItem
{
    [JsonPropertyName("count")]
    public int count;

    [JsonPropertyName("objective")]
    public ObjectiveType objective;

    [JsonPropertyName("order")]
    public int order;
}
