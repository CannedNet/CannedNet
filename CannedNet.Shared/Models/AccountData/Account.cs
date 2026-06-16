using System.Text.Json.Serialization;

namespace CannedNet;

public class Account
{
    [JsonPropertyName("accountId")] public int? AccountId { get; set; }
    [JsonPropertyName("profileImage")]  public string? ProfileImage { get; set; }
    [JsonPropertyName("isJunior")]  public bool IsJunior { get; set; } 
    [JsonPropertyName("platforms")] public int? Platforms { get; set; }
    [JsonPropertyName("personalPronouns")] public int? PersonalPronouns { get; set; }
    [JsonPropertyName("identityFlags")] public int? IdentityFlags { get; set; }
    [JsonPropertyName("username")] public string? Username { get; set; }
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
}