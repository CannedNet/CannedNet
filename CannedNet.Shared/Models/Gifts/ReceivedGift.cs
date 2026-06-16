using System.Text.Json.Serialization;

namespace CannedNet.Models;

public class ReceivedGift
{
    public int Id { get; set; }
    
    [JsonPropertyName("receiverAccountId")]
    public int ReceiverAccountId { get; set; }
    
    [JsonPropertyName("fromPlayerId")]
    public int? FromPlayerId { get; set; }
    
    [JsonPropertyName("consumableItemDesc")]
    public string ConsumableItemDesc { get; set; } = string.Empty;
    
    [JsonPropertyName("avatarItemDesc")]
    public string AvatarItemDesc { get; set; } = string.Empty;
    
    [JsonPropertyName("avatarItemType")]
    public int AvatarItemType { get; set; }
    
    [JsonPropertyName("friendlyName")]
    public string FriendlyName { get; set; } = string.Empty;
    
    [JsonPropertyName("equipmentPrefabName")]
    public string EquipmentPrefabName { get; set; } = string.Empty;
    
    [JsonPropertyName("equipmentModificationGuid")]
    public string EquipmentModificationGuid { get; set; } = string.Empty;
    
    [JsonPropertyName("currencyType")]
    public int CurrencyType { get; set; }
    
    [JsonPropertyName("currency")]
    public int Currency { get; set; }
    
    [JsonPropertyName("xp")]
    public int Xp { get; set; }
    
    [JsonPropertyName("level")]
    public int Level { get; set; }
    
    [JsonPropertyName("platform")]
    public int Platform { get; set; } = -1;
    
    [JsonPropertyName("platformsToSpawnOn")]
    public int PlatformsToSpawnOn { get; set; } = -1;
    
    [JsonPropertyName("balanceType")]
    public int BalanceType { get; set; }
    
    [JsonPropertyName("giftContext")]
    public int GiftContext { get; set; }
    
    [JsonPropertyName("giftRarity")]
    public int GiftRarity { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("receivedAt")]
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("isConsumed")]
    public bool IsConsumed { get; set; } = false;
    
    [JsonPropertyName("consumedAt")]
    public DateTime? ConsumedAt { get; set; }
}



