using System.Text.Json;
using System.Text.Json.Serialization;
using CannedNet.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CannedNet.Services.Controllers;

[ApiController, Route("match")]
public class MatchmakingController : ControllerBase
{
    [HttpPost("player/login")]
    public async Task<IResult> PlayerLogin()
    {
        return Results.Ok();
    }

    [HttpPost("player/exclusivelogin")]
    public async Task<IResult> PlayerExclusiveLogin()
    {
        return Results.Json(new { errorCode = 0 });
    }

    [HttpGet("player")]
    public async Task<IResult> GetPlayer(AppDbContext db)
    {
        var id = HttpContext.Request.Query["id"].FirstOrDefault();
        if (string.IsNullOrEmpty(id) || !int.TryParse(id, out var accountId))
        {
            var json = System.IO.File.ReadAllText("JSON/getplayer.json");
            return Results.Content(json, "application/json");
        }

        var account = await db.Accounts.FindAsync(accountId);
        if (account == null)
        {
            var json = System.IO.File.ReadAllText("JSON/getplayer.json");
            return Results.Content(json, "application/json");
        }

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
    }

    [HttpPost("goto/room/{room}")]
    [Authorize]
    public async Task<IResult> GotoRoom(string room, AppDbContext db)
    {
        if (!int.TryParse(User.Identity?.Name, out var id))
            return Results.Unauthorized();

        HttpContext.Request.EnableBuffering();
        HttpContext.Request.Body.Position = 0;
        var form = await HttpContext.Request.ReadFormAsync();
        int joinMode = 0;
        if (int.TryParse(form["JoinMode"].ToString(), out var mode))
        {
            joinMode = mode;
        }
        HttpContext.Request.Body.Position = 0;

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
    }

    [HttpPost("matchmake/{room}")]
    [Authorize]
    public async Task<IResult> MatchmakeRoom(string room, AppDbContext db)
    {
        if (!int.TryParse(User.Identity?.Name, out var id))
            return Results.Unauthorized();

        HttpContext.Request.EnableBuffering();
        HttpContext.Request.Body.Position = 0;
        var form = await HttpContext.Request.ReadFormAsync();
        int joinMode = 0;
        if (int.TryParse(form["JoinMode"].ToString(), out var mode))
        {
            joinMode = mode;
        }
        HttpContext.Request.Body.Position = 0;

        Room? roomData = null;
        var roomLower = room.ToLower();

        if (roomLower == "dorm")
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
    }

    [HttpPost("matchmake/none")]
    public async Task<IResult> MatchmakeNone()
    {
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
    }

    [HttpPost("player/heartbeat")]
    [Authorize]
    public async Task<IResult> PlayerHeartbeat(AppDbContext db)
    {
        if (!int.TryParse(User.Identity?.Name, out var id))
            return Results.Unauthorized();

        HttpContext.Request.EnableBuffering();
        HttpContext.Request.Body.Position = 0;
        using var reader = new StreamReader(HttpContext.Request.Body);
        var body = await reader.ReadToEndAsync();

        HttpContext.Request.Body.Position = 0;
        var form = await HttpContext.Request.ReadFormAsync();
        var loginLock = form["LoginLock"].ToString();

        HeartbeatRequest? heartbeat = null;
        if (!string.IsNullOrWhiteSpace(body) && body.TrimStart().StartsWith("{"))
        {
            heartbeat = JsonSerializer.Deserialize<HeartbeatRequest>(body);
        }

        if (heartbeat == null)
            heartbeat = new HeartbeatRequest();

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
    }

    [HttpPut("player/statusvisibility")]
    public async Task<IResult> PlayerStatusVisibility()
    {
        return Results.Ok();
    }

    [HttpPost("roominstance/{id}/reportjoinresult")]
    public async Task<IResult> ReportJoinResult()
    {
        return Results.Ok();
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
