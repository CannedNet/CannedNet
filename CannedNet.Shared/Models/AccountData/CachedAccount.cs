using System.Text.Json.Serialization;

namespace CannedNet;

public class CachedLogin
{
    [JsonIgnore]
    public int Id { get; set; }
    
    [JsonPropertyName("accountId")] 
    public int AccountId { get; set; }
    
    [JsonPropertyName("platform")] 
    public PlatformType Platform { get; set; }
    
    [JsonPropertyName("platformId")] 
    public string PlatformID { get; set; }
    
    [JsonPropertyName("lastLoginTime")] 
    public DateTime LastLoginTime { get; set; }
    
    [JsonPropertyName("requirePassword")] 
    public bool RequirePassword { get; set; } = false;
}