using System.Text.Json.Serialization;

namespace CannedNet.Models;

public class ConsumableItem
{
    public int Id { get; set; }
    
    public int OwnerAccountId { get; set; }
    
    [JsonPropertyName("Ids")]
    public List<int> Ids { get; set; } = new();
    
    [JsonPropertyName("CreatedAts")]
    public List<DateTime> CreatedAts { get; set; } = new();
    
    [JsonPropertyName("ConsumableItemDesc")]
    public string ConsumableItemDesc { get; set; } = string.Empty;
    
    [JsonPropertyName("Count")]
    public int Count { get; set; }
    
    [JsonPropertyName("InitialCount")]
    public int InitialCount { get; set; }
    
    [JsonPropertyName("IsActive")]
    public bool IsActive { get; set; }
    
    [JsonPropertyName("ActiveDurationMinutes")]
    public int ActiveDurationMinutes { get; set; }
    
    [JsonPropertyName("IsTransferable")]
    public bool IsTransferable { get; set; }
}

