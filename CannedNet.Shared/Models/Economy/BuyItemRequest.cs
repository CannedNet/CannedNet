using System.Text.Json.Serialization;

namespace CannedNet.Models;

public class BuyItemRequest
{
    [JsonPropertyName("StorefrontType")]
    public int StorefrontType { get; set; }
    
    [JsonPropertyName("PurchasableItemId")]
    public int PurchasableItemId { get; set; }
    
    [JsonPropertyName("CurrencyType")]
    public int CurrencyType { get; set; }
    
    [JsonPropertyName("CouponConsumablePlayerMappingId")]
    public int? CouponConsumablePlayerMappingId { get; set; }
    
    [JsonPropertyName("Gift")]
    public GiftRequest? Gift { get; set; }
}

public class GiftRequest
{
    [JsonPropertyName("ToPlayerId")]
    public int ToPlayerId { get; set; }
    
    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("Anonymous")]
    public bool Anonymous { get; set; }
    
    [JsonPropertyName("GiftContext")]
    public int GiftContext { get; set; }
}

