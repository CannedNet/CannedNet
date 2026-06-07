using CannedNet.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CannedNet.Services.Controllers;

[ApiController, Route("rooms")]
public class RoomsController : ControllerBase
{
    [HttpGet("rooms")]
    public async Task<IResult> GetRooms(AppDbContext db)
    {
        var idParam = HttpContext.Request.Query["id"].FirstOrDefault();
        var nameParam = HttpContext.Request.Query["name"].FirstOrDefault();

        if (string.IsNullOrEmpty(idParam) && string.IsNullOrEmpty(nameParam))
            return Results.BadRequest("Either 'id' or 'name' query parameter is required");

        Room? result = null;

        if (!string.IsNullOrEmpty(idParam))
        {
            var ids = idParam.Split(',').Select(s => int.TryParse(s.Trim(), out var i) ? i : -1).Where(i => i != -1).ToList();
            result = db.Rooms.FirstOrDefault(r => ids.Contains(r.RoomId));
        }
        else if (!string.IsNullOrEmpty(nameParam))
        {
            var name = nameParam.Trim().ToLower();
            result = db.Rooms.FirstOrDefault(r => r.Name.ToLower() == name);
        }

        if (result == null)
            return Results.Json(new { });

        return Results.Json(await BuildRoomResponse(result, db));
    }

    [HttpGet("rooms/{roomId:int}")]
    public async Task<IResult> GetRoomById(int roomId, AppDbContext db)
    {
        var result = await db.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
        if (result == null)
            return Results.NotFound();

        return Results.Json(await BuildRoomResponse(result, db));
    }

    [HttpGet("roomserver/rooms/createdby/me")]
    public async Task<IResult> GetRoomsCreatedByMe()
    {
        var json = System.IO.File.ReadAllText("JSON/ownedrooms.json");
        return Results.Content(json, "application/json");
    }

    private static async Task<object> BuildRoomResponse(Room result, AppDbContext db)
    {
        var roomId = result.RoomId;
        var subRooms = await db.SubRooms.Where(s => s.RoomId == roomId).Select(s => new
        {
            s.SubRoomId,
            s.Name,
            s.DataBlob,
            s.IsSandbox,
            s.MaxPlayers,
            s.Accessibility,
            s.UnitySceneId,
            DataSavedAt = s.DataSavedAt
        }).ToListAsync();
        var roles = await db.RoomRoles.Where(r => r.RoomId == roomId).Select(r => new
        {
            r.AccountId,
            r.Role,
            r.InvitedRole
        }).ToListAsync();
        var loadScreens = await db.LoadScreens.Where(l => l.RoomId == roomId).Select(l => new
        {
            l.ImageUrl,
            l.Tooltip,
            l.IsThumbnail
        }).ToListAsync();
        var promoImages = await db.PromoImages.Where(p => p.RoomId == roomId).Select(p => new
        {
            p.ImageUrl,
            p.Tooltip,
            p.SortOrder
        }).ToListAsync();
        var promoExternalContent = await db.PromoExternalContents.Where(p => p.RoomId == roomId).Select(p => new
        {
            p.Type,
            p.Url,
            p.Tooltip
        }).ToListAsync();

        return new
        {
            result.RoomId,
            result.Name,
            result.Description,
            result.CreatorAccountId,
            result.ImageName,
            result.State,
            result.Accessibility,
            result.SupportsLevelVoting,
            result.IsRRO,
            result.IsDorm,
            result.CloningAllowed,
            result.SupportsVRLow,
            result.SupportsQuest2,
            result.SupportsMobile,
            result.SupportsScreens,
            result.SupportsWalkVR,
            result.SupportsTeleportVR,
            result.SupportsJuniors,
            result.MinLevel,
            result.WarningMask,
            result.CustomWarning,
            result.DisableMicAutoMute,
            result.DisableRoomComments,
            result.EncryptVoiceChat,
            result.CreatedAt,
            Stats = new { CheerCount = 0, FavoriteCount = 0, VisitorCount = 0, VisitCount = 0 },
            SubRooms = subRooms,
            Roles = roles,
            LoadScreens = loadScreens,
            PromoImages = promoImages,
            PromoExternalContent = promoExternalContent,
            Tags = new object[0]
        };
    }
}
