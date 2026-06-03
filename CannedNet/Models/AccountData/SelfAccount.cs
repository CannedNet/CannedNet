using System.Text.Json.Serialization;

namespace CannedNet;

public class SelfAccount : Account
{
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("phone")] public string? Phone { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("juniorState")] public JuniorState? JuniorState { get; set; }
    [JsonPropertyName("birthday")] public DateTime? Birthday { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("parentAccountId")] public string? ParentAccountId { get; set; }
    [JsonPropertyName("availableUsernameChanges")] public int? AvailableUsernameChanges { get; set; }
}