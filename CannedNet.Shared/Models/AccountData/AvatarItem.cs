using System.Text.Json.Serialization;

namespace CannedNet;

public class AvatarItem
{
    public int Id { get; set; }
    
    [JsonPropertyName("avatarItemType")]
    public int AvatarItemType { get; set; }
    
    [JsonPropertyName("avatarItemDesc")]
    public string AvatarItemDesc { get; set; } = "";
    
    [JsonPropertyName("platformMask")]
    public int PlatformMask { get; set; } = -1;
    
    [JsonPropertyName("friendlyName")]
    public string FriendlyName { get; set; } = "";
    
    [JsonPropertyName("tooltip")]
    public string Tooltip { get; set; } = "";
    
    [JsonPropertyName("rarity")]
    public int Rarity { get; set; } = 0;
    
    public int OwnerAccountId { get; set; }
}
