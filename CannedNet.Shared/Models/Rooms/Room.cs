using System.Text.Json.Serialization;

namespace CannedNet;

public class RoomTag
{
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = "";
    [JsonPropertyName("type")]
    public int Type { get; set; }
}

public class Room
{
    public int Id { get; set; }
    [JsonPropertyName("roomId")]
    public int RoomId { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
    [JsonPropertyName("creatorAccountId")]
    public int CreatorAccountId { get; set; }
    [JsonPropertyName("imageName")]
    public string ImageName { get; set; } = "";
    [JsonPropertyName("state")]
    public int State { get; set; }
    [JsonPropertyName("accessibility")]
    public int Accessibility { get; set; }
    [JsonPropertyName("supportsLevelVoting")]
    public bool SupportsLevelVoting { get; set; }
    [JsonPropertyName("isRRO")]
    public bool IsRRO { get; set; }
    [JsonPropertyName("isDorm")]
    public bool IsDorm { get; set; }
    [JsonPropertyName("cloningAllowed")]
    public bool CloningAllowed { get; set; }
    [JsonPropertyName("supportsVRLow")]
    public bool SupportsVRLow { get; set; }
    [JsonPropertyName("supportsQuest2")]
    public bool SupportsQuest2 { get; set; }
    [JsonPropertyName("supportsMobile")]
    public bool SupportsMobile { get; set; }
    [JsonPropertyName("supportsScreens")]
    public bool SupportsScreens { get; set; }
    [JsonPropertyName("supportsWalkVR")]
    public bool SupportsWalkVR { get; set; }
    [JsonPropertyName("supportsTeleportVR")]
    public bool SupportsTeleportVR { get; set; }
    [JsonPropertyName("supportsJuniors")]
    public bool SupportsJuniors { get; set; }
    [JsonPropertyName("minLevel")]
    public int MinLevel { get; set; }
    [JsonPropertyName("warningMask")]
    public int WarningMask { get; set; }
    [JsonPropertyName("customWarning")]
    public string? CustomWarning { get; set; }
    [JsonPropertyName("disableMicAutoMute")]
    public bool DisableMicAutoMute { get; set; }
    [JsonPropertyName("disableRoomComments")]
    public bool DisableRoomComments { get; set; }
    [JsonPropertyName("encryptVoiceChat")]
    public bool EncryptVoiceChat { get; set; }
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
    [JsonPropertyName("tags")]
    public string Tags { get; set; } = "[]";
}

public class SubRoom
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    [JsonPropertyName("subRoomId")]
    public int SubRoomId { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    [JsonPropertyName("dataBlob")]
    public string DataBlob { get; set; } = "";
    [JsonPropertyName("isSandbox")]
    public bool IsSandbox { get; set; }
    [JsonPropertyName("maxPlayers")]
    public int MaxPlayers { get; set; }
    [JsonPropertyName("accessibility")]
    public int Accessibility { get; set; }
    [JsonPropertyName("unitySceneId")]
    public string UnitySceneId { get; set; } = "";
    [JsonPropertyName("dataSavedAt")]
    public DateTime? DataSavedAt { get; set; }
}

public class LoadScreen
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = "";
    [JsonPropertyName("tooltip")]
    public string Tooltip { get; set; } = "";
    [JsonPropertyName("isThumbnail")]
    public bool IsThumbnail { get; set; }
}

public class PromoImage
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = "";
    [JsonPropertyName("tooltip")]
    public string Tooltip { get; set; } = "";
    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }
}

public class PromoExternalContent
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    [JsonPropertyName("type")]
    public int Type { get; set; }
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
    [JsonPropertyName("tooltip")]
    public string Tooltip { get; set; } = "";
}

public class RoomRole
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }
    [JsonPropertyName("role")]
    public int Role { get; set; }
    [JsonPropertyName("invitedRole")]
    public int InvitedRole { get; set; }
}
