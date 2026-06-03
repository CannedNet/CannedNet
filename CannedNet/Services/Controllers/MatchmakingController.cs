using System.Text.Json;
using System.Text.Json.Serialization;
using CannedNet.Data;
using Microsoft.EntityFrameworkCore;
using CannedNet.Services;
using CannedNet.Services.Infrastructure;

namespace CannedNet.Services.Controllers;

public class MatchmakingController
{
    public WebApplicationBuilder Initialize(string[]? args = null) => ServiceExtensions.CreateRecNetBuilder(args);

    public void MapEndpoints(WebApplication app)
    {
        var jwtService = app.Services.GetRequiredService<JwtTokenService>();
        
        app.MapPost("/player/login", () => Results.Ok());

        app.MapPost("/player/exclusivelogin", (HttpRequest request) =>
        {
            return Results.Json(new { errorCode = 0 });
        });
        
        app.MapGet("/player", async (HttpRequest request, AppDbContext db) =>
        {
            var id = request.Query["id"].FirstOrDefault();
            if (string.IsNullOrEmpty(id) || !int.TryParse(id, out var accountId))
            {
                var json = File.ReadAllText("JSON/getplayer.json");
                return Results.Content(json, "application/json");
            }

            var account = await db.Accounts.FindAsync(accountId);
            if (account == null)
            {
                var json = File.ReadAllText("JSON/getplayer.json");
                return Results.Content(json, "application/json");
            }

            // Get the player's active room instance
            var roomInstance = await db.RoomInstances
                .Where(r => r.OwnerAccountId == accountId)
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            var result = new List<object>
            {
                new
                {
                    playerId = account.AccountId,
                    statusVisibility = 0,
                    deviceClass = 0,
                    vrMovementMode = 1,
                    roomInstance = roomInstance != null ? new
                    {
                        roomInstanceId = roomInstance.Id > 0 ? roomInstance.Id : roomInstance.roomInstanceId,
                        roomId = roomInstance.roomId,
                        subRoomId = roomInstance.subRoomId,
                        roomInstanceType = roomInstance.roomInstanceType,
                        location = roomInstance.location,
                        dataBlob = roomInstance.dataBlob,
                        eventId = roomInstance.eventId,
                        clubId = roomInstance.clubId,
                        roomCode = roomInstance.roomCode,
                        photonRegionId = roomInstance.photonRegionId,
                        photonRoomId = roomInstance.photonRoomId,
                        name = roomInstance.name,
                        maxCapacity = roomInstance.maxCapacity,
                        isFull = roomInstance.isFull,
                        isPrivate = roomInstance.isPrivate,
                        isInProgress = roomInstance.isInProgress,
                        EncryptVoiceChat = roomInstance.EncryptVoiceChat
                    } : (object?)null,
                    isOnline = roomInstance != null,
                    appVersion = "",
                    platform = account.Platforms ?? 0
                }
            };

            return Results.Json(result);
        });

        app.MapPost("/goto/room/{room}", async (HttpRequest request, string room, AppDbContext db) =>
        {
            var authHeader = request.Headers.Authorization.ToString();
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Results.Unauthorized();

            var token = authHeader.Substring("Bearer ".Length);
            var accountId = jwtService.ValidateAndGetAccountId(token);

            if (string.IsNullOrEmpty(accountId))
                return Results.Unauthorized();

            if (!int.TryParse(accountId.AsSpan(), out var id))
                return Results.Unauthorized();
            
            request.EnableBuffering();
            request.Body.Position = 0;
            var form = await request.ReadFormAsync();
            int joinMode = 0;
            if (int.TryParse(form["JoinMode"].ToString(), out var mode))
            {
                joinMode = mode;
            }
            request.Body.Position = 0;
            
            Room? roomData = null;
            var roomLower = room.ToLower();
            
            if (roomLower == "dormroom")
            {
                roomData = await db.Rooms.FirstOrDefaultAsync(r => r.IsDorm && r.CreatorAccountId == id);
            }
            else if (int.TryParse(room, out var roomId))
            {
                roomData = await db.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
            }
            
            if (roomData == null)
            {
                roomData = await db.Rooms.FirstOrDefaultAsync(r => r.Name.ToLower() == roomLower);
            }

            if (roomData == null)
            {
                return Results.NotFound("Room not found");
            }

            var subRoom = await db.SubRooms.FirstOrDefaultAsync(s => s.RoomId == roomData.RoomId);
            
            string location = subRoom?.UnitySceneId ?? "";
            int subRoomId = subRoom?.SubRoomId ?? 0;
            int maxCapacity = subRoom?.MaxPlayers ?? 4;


            RoomInstance? instanceToUse;
            string photonRoomId;
            
            bool isPrivateInstance = joinMode == 2 || roomData.IsDorm;

            var existingInstance = await db.RoomInstances.FirstOrDefaultAsync(r => r.OwnerAccountId == id && r.roomId == roomData.RoomId);
            
            if (existingInstance != null)
            {
                photonRoomId = existingInstance.photonRoomId ?? Guid.NewGuid().ToString();
                existingInstance.roomInstanceId = existingInstance.Id;
                existingInstance.roomId = roomData.RoomId;
                existingInstance.subRoomId = subRoomId;
                existingInstance.location = location;
                existingInstance.dataBlob = "";
                existingInstance.photonRegionId = "us";
                existingInstance.photonRoomId = photonRoomId;
                existingInstance.name = roomData.Name;
                existingInstance.maxCapacity = maxCapacity;
                existingInstance.isFull = false;
                existingInstance.isPrivate = isPrivateInstance;
                instanceToUse = existingInstance;
            }
            else
            {
                photonRoomId = Guid.NewGuid().ToString();
                
                var oldInstances = await db.RoomInstances
                    .Where(r => r.OwnerAccountId == id && r.roomId != roomData.RoomId)
                    .ToListAsync();
                db.RoomInstances.RemoveRange(oldInstances);
                
                var publicInstance = await db.RoomInstances
                    .FirstOrDefaultAsync(r => r.roomId == roomData.RoomId && !r.isPrivate && !r.isFull);
                
                if (publicInstance != null)
                {
                    photonRoomId = publicInstance.photonRoomId ?? Guid.NewGuid().ToString();
                }
                
                instanceToUse = new RoomInstance
                {
                    OwnerAccountId = id,
                    roomInstanceId = 1,
                    roomId = roomData.RoomId,
                    subRoomId = subRoomId,
                    roomInstanceType = 2,
                    location = location,
                    dataBlob = "",
                    photonRegionId = "us",
                    photonRoomId = photonRoomId,
                    name = roomData.Name,
                    maxCapacity = maxCapacity,
                    isFull = false,
                    isPrivate = isPrivateInstance,
                    isInProgress = false,
                    EncryptVoiceChat = roomData.EncryptVoiceChat
                };
                db.RoomInstances.Add(instanceToUse);
            }
            
            await db.SaveChangesAsync();

            if (instanceToUse.Id == 0)
            {
                instanceToUse = await db.RoomInstances.FirstOrDefaultAsync(r => r.OwnerAccountId == id && r.roomId == roomData.RoomId);
            }
            
            var response = new
            {
                errorCode = 0,
                roomInstance = new
                {
                    roomInstanceId = instanceToUse?.Id ?? 1,
                    roomId = instanceToUse?.roomId ?? roomData.RoomId,
                    subRoomId = instanceToUse?.subRoomId ?? subRoomId,
                    roomInstanceType = instanceToUse?.roomInstanceType ?? 2,
                    location = instanceToUse?.location ?? location,
                    dataBlob = instanceToUse?.dataBlob ?? "",
                    eventId = instanceToUse?.eventId ?? 0,
                    clubId = instanceToUse?.clubId ?? 0,
                    roomCode = instanceToUse?.roomCode ?? "",
                    photonRegionId = instanceToUse?.photonRegionId ?? "us",
                    photonRoomId = instanceToUse?.photonRoomId ?? photonRoomId,
                    name = instanceToUse?.name ?? roomData.Name,
                    maxCapacity = instanceToUse?.maxCapacity ?? maxCapacity,
                    isFull = instanceToUse?.isFull ?? false,
                    isPrivate = isPrivateInstance,
                    isInProgress = instanceToUse?.isInProgress ?? false,
                    EncryptVoiceChat = instanceToUse?.EncryptVoiceChat ?? roomData.EncryptVoiceChat
                }
            };
            return Results.Json(response);
        });
        
        app.MapPost("/goto/none", (HttpRequest request) =>
        {
            // dormroom location id : 76d98498-60a1-430c-ab76-b54a29b7a163
            // offline dorm
            
            return Results.Json(new
            {
                errorCode = 0,
                roomInstance = new
                {
                    roomInstanceId = 1,
                    roomId = 1,
                    subRoomId = 1,
                    roomInstanceType = 2,
                    location = "76d98498-60a1-430c-ab76-b54a29b7a163",
                    dataBlob = "",
                    eventId = 0,
                    clubId = 0,
                    photonRegionId = "us",
                    photonRoomId = Guid.NewGuid(),
                    name = "DormRoom",
                    maxCapacity = 4,
                    isFull = false,
                    isPrivate = true,
                    isInProgress = false,
                    EncryptVoiceChat = false
                }
            });
        });

        app.MapPost("/player/heartbeat", async (HttpRequest request, AppDbContext db) =>
        {
            var authHeader = request.Headers.Authorization.ToString();
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Results.Unauthorized();

            var token = authHeader.Substring("Bearer ".Length);
            var accountId = jwtService.ValidateAndGetAccountId(token);

            if (string.IsNullOrEmpty(accountId))
                return Results.Unauthorized();

            if (!int.TryParse(accountId.AsSpan(), out var id))
                return Results.Unauthorized();

            request.EnableBuffering();
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();
            
            request.Body.Position = 0;
            var form = await request.ReadFormAsync();
            var loginLock = form["LoginLock"].ToString();
            
            HeartbeatRequest? heartbeat = null;
            if (!string.IsNullOrWhiteSpace(body) && body.TrimStart().StartsWith("{"))
            {
                heartbeat = JsonSerializer.Deserialize<HeartbeatRequest>(body);
            }
            
            if (heartbeat == null)
                heartbeat = new HeartbeatRequest();

            // Get the player's active room instance (most recent one)
            var roomInstance = await db.RoomInstances
                .Where(r => r.OwnerAccountId == id)
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();


            return Results.Json(new
            {
                playerId = heartbeat.playerId != 0 ? heartbeat.playerId : id,
                statusVisibility = heartbeat.statusVisibility,
                deviceClass = heartbeat.deviceClass,
                vrMovementMode = heartbeat.vrMovementMode != 0 ? heartbeat.vrMovementMode : 1,
                roomInstance = roomInstance != null ? new
                {
                    roomInstanceId = roomInstance.Id > 0 ? roomInstance.Id : roomInstance.roomInstanceId,
                    roomId = roomInstance.roomId,
                    subRoomId = roomInstance.subRoomId,
                    roomInstanceType = roomInstance.roomInstanceType,
                    location = roomInstance.location,
                    dataBlob = roomInstance.dataBlob,
                    eventId = roomInstance.eventId,
                    clubId = roomInstance.clubId,
                    roomCode = roomInstance.roomCode,
                    photonRegionId = roomInstance.photonRegionId,
                    photonRoomId = roomInstance.photonRoomId,
                    name = roomInstance.name,
                    maxCapacity = roomInstance.maxCapacity,
                    isFull = roomInstance.isFull,
                    isPrivate = roomInstance.isPrivate,
                    isInProgress = roomInstance.isInProgress,
                    EncryptVoiceChat = roomInstance.EncryptVoiceChat
                } : null,
                isOnline = roomInstance != null,
                appVersion = heartbeat.appVersion ?? "",
                platform = heartbeat.platform
            });
        });
        app.MapPut("/player/statusvisibility", async (HttpRequest request, AppDbContext db) =>
        {
            // TODO ADD FUNCTIONALITY
            return Results.Ok();
        });
        app.MapPost("/roominstance/{id}/reportjoinresult", async (HttpRequest request, AppDbContext db) =>
        {
            return Results.Ok();
        });
    }
}

public class HeartbeatRequest
{
    [JsonPropertyName("playerId")]
    public int playerId { get; set; }

    [JsonPropertyName("statusVisibility")]
    public int statusVisibility { get; set; }

    [JsonPropertyName("deviceClass")]
    public int deviceClass { get; set; }

    [JsonPropertyName("vrMovementMode")]
    public int vrMovementMode { get; set; }

    [JsonPropertyName("roomInstance")]
    public RoomInstanceInfo? roomInstance { get; set; }

    [JsonPropertyName("isOnline")]
    public bool isOnline { get; set; }

    [JsonPropertyName("appVersion")]
    public string? appVersion { get; set; }

    [JsonPropertyName("platform")]
    public int platform { get; set; }
}

public class RoomInstanceInfo
{
    [JsonPropertyName("roomInstanceId")]
    public int roomInstanceId { get; set; }

    [JsonPropertyName("roomId")]
    public int roomId { get; set; }

    [JsonPropertyName("subRoomId")]
    public int subRoomId { get; set; }

    [JsonPropertyName("roomInstanceType")]
    public int roomInstanceType { get; set; }

    [JsonPropertyName("location")]
    public string? location { get; set; }

    [JsonPropertyName("dataBlob")]
    public string? dataBlob { get; set; }

    [JsonPropertyName("eventId")]
    public int eventId { get; set; }

    [JsonPropertyName("clubId")]
    public int clubId { get; set; }

    [JsonPropertyName("roomCode")]
    public string? roomCode { get; set; }

    [JsonPropertyName("photonRegionId")]
    public string? photonRegionId { get; set; }

    [JsonPropertyName("photonRoomId")]
    public string? photonRoomId { get; set; }

    [JsonPropertyName("name")]
    public string? name { get; set; }

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
