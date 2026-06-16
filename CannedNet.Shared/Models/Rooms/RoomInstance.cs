using System.Text.Json.Serialization;

namespace CannedNet;

public class RoomInstance
{
    public int Id { get; set; }
    public int OwnerAccountId { get; set; }
    [JsonPropertyName("roomInstanceId")]
    public int roomInstanceId { get; set; }
    [JsonPropertyName("roomId")]
    public int roomId { get; set; }
    [JsonPropertyName("subRoomId")]
    public int subRoomId { get; set; }
    [JsonPropertyName("roomInstanceType")]
    public int roomInstanceType { get; set; }
    [JsonPropertyName("location")]
    public string location { get; set; } = "";
    [JsonPropertyName("dataBlob")]
    public string dataBlob { get; set; } = "";
    [JsonPropertyName("eventId")]
    public int eventId { get; set; }
    [JsonPropertyName("clubId")]
    public int clubId { get; set; }
    [JsonPropertyName("roomCode")]
    public string roomCode { get; set; } = "";
    [JsonPropertyName("photonRegionId")]
    public string photonRegionId { get; set; } = "";
    [JsonPropertyName("photonRoomId")]
    public string photonRoomId { get; set; } = "";
    [JsonPropertyName("name")]
    public string name { get; set; } = "";
    [JsonPropertyName("maxCapacity")]
    public int maxCapacity { get; set; }
    [JsonPropertyName("isFull")]
    public bool isFull { get; set; }
    [JsonPropertyName("isPrivate")]
    public bool isPrivate { get; set; }
    [JsonPropertyName("isInProgress")]
    public bool isInProgress { get; set; }
    [JsonPropertyName("EncryptVoiceChat")]
    public bool EncryptVoiceChat { get; set; }
}