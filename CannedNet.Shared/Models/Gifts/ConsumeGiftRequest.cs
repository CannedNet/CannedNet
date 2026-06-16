using System.Text.Json.Serialization;

namespace CannedNet.Models;

public class ConsumeGiftRequest
{
    [JsonPropertyName("GiftId")]
    public int GiftId { get; set; }
    
    [JsonPropertyName("Count")]
    public int Count { get; set; }
}

