using System.Text.Json.Serialization;

namespace CannedNet;

public class PlayerAvatar
{
    public int Id { get; set; }
    public int OwnerAccountId { get; set; }
    
    [JsonPropertyName("OutfitSelections")]
    public string OutfitSelections { get; set; } = "";
    
    [JsonPropertyName("FaceFeatures")]
    public string FaceFeatures { get; set; } = "{}";
    
    [JsonPropertyName("SkinColor")]
    public string SkinColor { get; set; } = "";
    
    [JsonPropertyName("HairColor")]
    public string HairColor { get; set; } = "";
}
