using System.Text.Json.Serialization;

namespace CannedNet.Models;

public class EarnableReward
{
    public int Id { get; set; }
    
    [JsonPropertyName("friendlyName")]
    public string FriendlyName { get; set; } = string.Empty;
    
    [JsonPropertyName("rewardContext")]
    public int RewardContext { get; set; }
    
    [JsonPropertyName("consumableItemDesc")]
    public string ConsumableItemDesc { get; set; } = string.Empty;
    
    [JsonPropertyName("avatarItemDesc")]
    public string AvatarItemDesc { get; set; } = string.Empty;
    
    [JsonPropertyName("avatarItemType")]
    public int AvatarItemType { get; set; }
    
    [JsonPropertyName("giftRarity")]
    public int GiftRarity { get; set; }
}

