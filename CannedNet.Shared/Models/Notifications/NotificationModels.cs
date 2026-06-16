using System.Text.Json.Serialization;

namespace CannedNet;

public class NotificationMessage
{
    [JsonPropertyName("notificationId")]
    public string? NotificationId { get; set; }

    [JsonPropertyName("notificationType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PushNotificationId? NotificationType { get; set; }

    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("fromAccountId")]
    public int? FromAccountId { get; set; }

    [JsonPropertyName("toAccountId")]
    public int? ToAccountId { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; set; }
}

public class SubscriptionUpdateProfile
{
    [JsonPropertyName("accountId")]
    public int? AccountId { get; set; }

    [JsonPropertyName("profileImage")]
    public string? ProfileImage { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }
}

public class SubscriptionUpdatePresence
{
    [JsonPropertyName("accountId")]
    public int? AccountId { get; set; }

    [JsonPropertyName("presence")]
    public string? Presence { get; set; }

    [JsonPropertyName("currentRoomId")]
    public string? CurrentRoomId { get; set; }

    [JsonPropertyName("currentWorldId")]
    public string? CurrentWorldId { get; set; }

    [JsonPropertyName("lastSeen")]
    public DateTime? LastSeen { get; set; }
}

public class SubscriptionUpdateGameSession
{
    [JsonPropertyName("gameSessionId")]
    public string? GameSessionId { get; set; }

    [JsonPropertyName("roomId")]
    public string? RoomId { get; set; }

    [JsonPropertyName("worldId")]
    public string? WorldId { get; set; }

    [JsonPropertyName("players")]
    public List<int>? Players { get; set; }
}

public class SubscriptionUpdateRoom
{
    [JsonPropertyName("roomId")]
    public string? RoomId { get; set; }

    [JsonPropertyName("worldId")]
    public string? WorldId { get; set; }

    [JsonPropertyName("roomName")]
    public string? RoomName { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }
}

public class RelationshipChangedNotification
{
    [JsonPropertyName("accountId")]
    public int? AccountId { get; set; }

    [JsonPropertyName("relationshipType")]
    public RelationshipType? RelationshipType { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class PresenceHeartbeatResponse
{
    [JsonPropertyName("currentRoomId")]
    public string? CurrentRoomId { get; set; }

    [JsonPropertyName("currentWorldId")]
    public string? CurrentWorldId { get; set; }

    [JsonPropertyName("presence")]
    public string? Presence { get; set; }
}

public class ModerationNotification
{
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("duration")]
    public int? Duration { get; set; }

    [JsonPropertyName("moderatedBy")]
    public string? ModeratedBy { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("gameSessionId")]
    public string? GameSessionId { get; set; }
}

public class GiftPackageNotification
{
    [JsonPropertyName("giftPackageId")]
    public string? GiftPackageId { get; set; }

    [JsonPropertyName("fromAccountId")]
    public int? FromAccountId { get; set; }

    [JsonPropertyName("giftType")]
    public string? GiftType { get; set; }
}

public class StorefrontBalanceNotification
{
    [JsonPropertyName("balance")]
    public decimal? Balance { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("purchaseId")]
    public string? PurchaseId { get; set; }
}

public class ConsumableMappingNotification
{
    [JsonPropertyName("mappingId")]
    public string? MappingId { get; set; }

    [JsonPropertyName("consumableId")]
    public string? ConsumableId { get; set; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }
}

public class PlayerEventNotification
{
    [JsonPropertyName("eventId")]
    public string? EventId { get; set; }

    [JsonPropertyName("eventName")]
    public string? EventName { get; set; }

    [JsonPropertyName("eventType")]
    public string? EventType { get; set; }

    [JsonPropertyName("worldId")]
    public string? WorldId { get; set; }

    [JsonPropertyName("responseStatus")]
    public string? ResponseStatus { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }
}

public class ChatMessageNotification
{
    [JsonPropertyName("channelId")]
    public string? ChannelId { get; set; }

    [JsonPropertyName("fromAccountId")]
    public int? FromAccountId { get; set; }

    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("channelType")]
    public string? ChannelType { get; set; }
}

public class CommunityBoardNotification
{
    [JsonPropertyName("boardId")]
    public string? BoardId { get; set; }

    [JsonPropertyName("announcementId")]
    public string? AnnouncementId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

public class InventionModerationNotification
{
    [JsonPropertyName("inventionId")]
    public string? InventionId { get; set; }

    [JsonPropertyName("moderationState")]
    public ModerationState? ModerationState { get; set; }
}
