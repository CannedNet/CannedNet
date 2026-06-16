using System.Text.Json.Serialization;

namespace CannedNet.Models;

public class BuyItemResponse
{
    [JsonPropertyName("BalanceUpdates")]
    public List<BalanceUpdate> BalanceUpdates { get; set; } = new();
    
    [JsonPropertyName("Balance")]
    public int Balance { get; set; }
    
    [JsonPropertyName("CurrencyType")]
    public int CurrencyType { get; set; }
    
    [JsonPropertyName("BalanceType")]
    public int BalanceType { get; set; }
    
    [JsonPropertyName("Platform")]
    public int Platform { get; set; }
}

public class BalanceUpdate
{
    [JsonPropertyName("UpdateResponse")]
    public int UpdateResponse { get; set; }
    
    [JsonPropertyName("Data")]
    public List<GiftData> Data { get; set; } = new();
}

public class GiftData
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }
    
    [JsonPropertyName("FromPlayerId")]
    public int? FromPlayerId { get; set; }
    
    [JsonPropertyName("ConsumableItemDesc")]
    public string ConsumableItemDesc { get; set; } = string.Empty;
    
    [JsonPropertyName("AvatarItemDesc")]
    public string AvatarItemDesc { get; set; } = string.Empty;
    
    [JsonPropertyName("AvatarItemType")]
    public int AvatarItemType { get; set; }
    
    [JsonPropertyName("FriendlyName")]
    public string FriendlyName { get; set; } = string.Empty;
    
    [JsonPropertyName("EquipmentPrefabName")]
    public string EquipmentPrefabName { get; set; } = string.Empty;
    
    [JsonPropertyName("EquipmentModificationGuid")]
    public string EquipmentModificationGuid { get; set; } = string.Empty;
    
    [JsonPropertyName("CurrencyType")]
    public int CurrencyType { get; set; }
    
    [JsonPropertyName("Currency")]
    public int Currency { get; set; }
    
    [JsonPropertyName("Xp")]
    public int Xp { get; set; }
    
    [JsonPropertyName("Level")]
    public int Level { get; set; }
    
    [JsonPropertyName("Platform")]
    public int Platform { get; set; }
    
    [JsonPropertyName("PlatformsToSpawnOn")]
    public int PlatformsToSpawnOn { get; set; }
    
    [JsonPropertyName("BalanceType")]
    public int BalanceType { get; set; }
    
    [JsonPropertyName("GiftContext")]
    public int GiftContext { get; set; }
    
    [JsonPropertyName("GiftRarity")]
    public int GiftRarity { get; set; }
    
    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;
}

