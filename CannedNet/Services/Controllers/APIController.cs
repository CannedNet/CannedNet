using CannedNet.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using CannedNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CannedNet.Services.Controllers;

[ApiController, Route("api")]
public class APIController : ControllerBase
{
    [HttpGet("config/v1/amplitude")]
    public async Task<IResult> Amplitude() {
        return Results.Ok(new {
            AmplitudeKey = "a",
            StatSigKey = "a",
            RudderStackKey = "a",
            UseRudderStack = false
        });
    }

    [HttpGet("config/v1/azurespeech")]
    public async Task<IResult> AzureSpeech() {
        return Results.Ok(new {
            Key = "dce8de5b297747d9b5bddcc7f19e8c5b",
            Region = "eastus",
            Enabled = false
        });
    }

    [HttpGet("config/v2")]
    public async Task<IResult> ConfigV2() => Results.Content(await System.IO.File.ReadAllTextAsync("JSON/configv2.json"), "application/json");

    [HttpGet("gameconfigs/v1/all")]
    public async Task<IResult> GameConfigsV1All() => Results.Content(await System.IO.File.ReadAllTextAsync("JSON/gameconfigs.json"), "application/json");
    
    [HttpGet("versioncheck/v4")]
    public async Task<IResult> VersionCheckV4() => Results.Ok(new {
        VersionStatus = 0,
        UpdateNotificationStage = 0,
        IsVersionIslanded = false,
        IsCrossPlayDisabled = false
    });

    [HttpGet("relationships/v2/get")]
    public async Task<IResult> GetRelationShipsV2() => Results.Content("[]", "application/json");

    [HttpGet("messages/v2/get")]
    public async Task<IResult> GetMessagesV2() => Results.Content("[]", "application/json");
    
    [HttpGet("playerReputation/v1/{id}")]
    public async Task<IResult> GetPlayersV1Reputation(string id) => Results.Content($"{{\"AccountId\":{id},\"Noteriety\":0,\"CheerGeneral\":0,\"CheerHelpful\":0,\"CheerCreative\":0,\"CheerGreatHost\":0,\"CheerSportsman\":0,\"CheerCredit\":20,\"SelectedCheer\":null}}", "application/json");
    
    [HttpGet("players/v1/progression/{id}")]
    public async Task<IResult> GetPlayersV1Progression(string id) => Results.Content($"{{\"PlayerId\":{id},\"Level\":1,\"XP\":0}}", "application/json");

    [HttpPost("playerReputation/v2/bulk")]
    public async Task<IResult> PostPlayerReputationV2Bulk(HttpRequest httpRequest, AppDbContext db) {
        /*var ids = await ParseFormIds(httpRequest);

            if (!ids.Any())
                return Results.Json(new List<object>());

            var reputations = ids.Select(id => new
            {
                AccountId = id,
                Noteriety = 0,
                CheerGeneral = 0,
                CheerHelpful = 0,
                CheerCreative = 0,
                CheerGreatHost = 0,
                CheerSportsman = 0,
                CheerCredit = 20,
                SelectedCheer = (object?)null
            }).ToList();

            return Results.Json(reputations);*/

        //TODO: implement real endpoint from grabbing from db
        string json = await System.IO.File.ReadAllTextAsync("JSON/bulkprogression.json");
        return Results.Content(json, "application/json");
    }

    [HttpPost("players/v2/progression/bulk")]
    public async Task<IResult> PostProgressionBulkV2(HttpRequest httpRequest, AppDbContext db) {
        List<int> ids = await ParseFormIds(httpRequest);
            
        if (!ids.Any())
            return Results.Json(new List<PlayerProgressionBulkResponse>());
            
        List<PlayerProgressionBulkResponse> progressions = await db.PlayerProgressions
            .Where(p => ids.Contains(p.PlayerId))
            .Select(p => new PlayerProgressionBulkResponse { PlayerId = p.PlayerId, Level = p.Level, Xp = p.Xp })
            .ToListAsync();
            
        return Results.Json(progressions);
    }

    [HttpPost("v1/progression/bulk")]
    public async Task<IResult> PostProgressionBulkV1(HttpRequest httpRequest, AppDbContext db) {
        List<int> ids = await ParseFormIds(httpRequest);

        if (!ids.Any())
            return Results.Json(new List<PlayerProgressionBulkResponse>());

        List<PlayerProgressionBulkResponse> progressions = await db.PlayerProgressions
            .Where(p => ids.Contains(p.PlayerId))
            .Select(p => new PlayerProgressionBulkResponse { PlayerId = p.PlayerId, Level = p.Level, Xp = p.Xp })
            .ToListAsync();

        return Results.Json(progressions);
    }



    [HttpPost("avatar/v2/set")]
    [Authorize]
    public async Task<IResult> SetAvatarV2(HttpRequest request, AppDbContext db, JwtTokenService jwtService)
    {
        int id = User.Identity.Name != null && int.TryParse(User.Identity.Name, out var parsedId) ? parsedId : -1;
        request.EnableBuffering();
        var avatarUpdate = await System.Text.Json.JsonSerializer.DeserializeAsync<PlayerAvatar>(request.Body);
            
        if (avatarUpdate == null)
            return Results.BadRequest();
            
        var avatar = await db.PlayerAvatars
            .FirstOrDefaultAsync(a => a.OwnerAccountId == id);
            
        if (avatar == null)
        {
            avatar = new PlayerAvatar { OwnerAccountId = id };
            db.PlayerAvatars.Add(avatar);
        }
            
        avatar.OutfitSelections = avatarUpdate.OutfitSelections;
        avatar.FaceFeatures = avatarUpdate.FaceFeatures;
        avatar.SkinColor = avatarUpdate.SkinColor;
        avatar.HairColor = avatarUpdate.HairColor;
            
        await db.SaveChangesAsync();
        return Results.Ok(avatar);
    }
    
    [HttpGet("PlayerReporting/v1/moderationBlockDetails")]
    public IResult GetModerationBlockDetails()
    {
        return Results.Content("{\"ReportCategory\":0,\"Duration\":0,\"GameSessionId\":0,\"IsHostKick\":false,\"Message\":\"\",\"PlayerIdReporter\":null,\"IsBan\":false}", "application/json");
    }

    [HttpGet("PlayerReporting/v1/voteToKickReasons")]
    public IResult GetVoteToKickReasons()
    {
        var json = System.IO.File.ReadAllText("JSON/vtkreasons.json");
        return Results.Content(json, "application/json");
    }

    [HttpGet("settings/v2")]
    [Authorize]
    public async Task<IResult> GetSettings(HttpRequest request, AppDbContext db, JwtTokenService jwtService)
    {
        int id = User.Identity.Name != null && int.TryParse(User.Identity.Name, out var parsedId) ? parsedId : -1;
            
        var settings = await db.PlayerSettings
            .Where(s => s.PlayerId == id)
            .ToListAsync();
            
        if (!settings.Any())
        {
            var defaults = new List<PlayerSetting>
            {
                new() { PlayerId = id, Key = "Recroom.OOBE", Value = "77" },
                new() { PlayerId = id, Key = "SplitTestAssignedSegments", Value = "1|{\"SplitTesting+PhotonMaxDatagrams_2021_01_11\":\"Off\",\"SplitTesting+Curated_Rooms_2020_08_06\":\"Off\",\"SplitTesting+RoomRecommendationsType_2020_08_14\":\"Aug14MinVisitors35000\"}" },
                new() { PlayerId = id, Key = "PlayerSessionCount", Value = "13" },
                new() { PlayerId = id, Key = "TUTORIAL_COMPLETE_MASK", Value = "11" },
                new() { PlayerId = id, Key = "BACKPACK_FAVORITE_TOOL", Value = "1" },
                new() { PlayerId = id, Key = "VoiceChat", Value = "2" },
                new() { PlayerId = id, Key = "VRAUTOSPRINT", Value = "1" },
                new() { PlayerId = id, Key = "VR_MOVEMENT_MODE", Value = "0" },
                new() { PlayerId = id, Key = "COMFORT_SPRINT", Value = "0" },
                new() { PlayerId = id, Key = "COMFORT_WALK", Value = "0" },
                new() { PlayerId = id, Key = "COMFORT_VEHICLES", Value = "0" },
                new() { PlayerId = id, Key = "COMFORT_FLY", Value = "0" },
                new() { PlayerId = id, Key = "COMFORT_ROTATE", Value = "0" },
                new() { PlayerId = id, Key = "COMFORT_FORCES", Value = "0" },
                new() { PlayerId = id, Key = "COMFORT_FALL", Value = "0" },
                new() { PlayerId = id, Key = "COMFORT_TELEPORT", Value = "0" },
                new() { PlayerId = id, Key = "ROTATE_IN_PLACE_ENABLED", Value = "1" },
                new() { PlayerId = id, Key = "ROTATION_INCREMENT", Value = "2" },
                new() { PlayerId = id, Key = "CONTINUOUS_ROTATION_MODE", Value = "1" },
                new() { PlayerId = id, Key = "DONT_LOCK_TOOLS_TO_HAND", Value = "0" },
                new() { PlayerId = id, Key = "QualitySettings", Value = "2" },
                new() { PlayerId = id, Key = "TeleportBuffer", Value = "0" },
                new() { PlayerId = id, Key = "IgnoreBuffer", Value = "1" },
                new() { PlayerId = id, Key = "FIRST_TIME_IN_FLAGS", Value = "0" },
                new() { PlayerId = id, Key = "ShowRoomCenter", Value = "1" },
                new() { PlayerId = id, Key = "USER_TRACKING", Value = "1" },
                new() { PlayerId = id, Key = "STABILIZE_HANDS", Value = "0" },
                new() { PlayerId = id, Key = "MakerPen_SnappingMode", Value = "2" },
                new() { PlayerId = id, Key = "Recroom.ChallengeMap", Value = "17" },
                new() { PlayerId = id, Key = "VoiceFilter2", Value = "1" },
                new() { PlayerId = id, Key = "SFX_VOLUME_PERCENT_PREF", Value = "1" },
            };
            db.PlayerSettings.AddRange(defaults);
            await db.SaveChangesAsync();
            settings = defaults;
        }
    
        return Results.Json(settings);
    }
    
    [HttpGet("accounts/v1/getBio")]
    [Authorize]
    public async Task<IResult> GetAccountBio(HttpRequest request, AppDbContext db)
    {
        int accountId = User.Identity.Name != null && int.TryParse(User.Identity.Name, out var parsedId) ? parsedId : -1;

        var bio = await db.PlayerBios
            .Where(s => accountId == accountId)
            .ToListAsync();
            
        return Results.Json(bio);
    }
    

    [HttpGet("communityboard/v2/current")]
    public async Task<IResult> CommunityBoardCurrent() {
        var json = System.IO.File.ReadAllText("JSON/communityboard.json");
        return Results.Content(json, "application/json");
    }

    [HttpGet("playerevents/v1/all")]
    public async Task<IResult> PlayerEventsAll() {
        return Results.Content("{\"Created\":[],\"Responses\":[]}", "application/json");
    }

    [HttpGet("storefronts/v1/p2p/betaEnabled")]
    public IResult StorefrontsP2PBetaEnabled() {
        return Results.Content("false", "application/json");
    }

    [HttpGet("announcement/v1/get")]
    public async Task<IResult> AnnouncementGet() {
        var json = System.IO.File.ReadAllText("JSON/announcements.json");
        return Results.Content(json, "application/json");
    }

    [HttpGet("quickPlay/v1/getandclear")]
    public async Task<IResult> QuickPlayGetAndClear() {
        return Results.Content("{\"RoomName\":null,\"ActionCode\":null,\"TargetPlayerId\":null}", "application/json");
    }

    [HttpGet("roomkeys/v1/room")]
    public async Task<IResult> RoomKeysRoom() {
        var roomid = Request.Query["roomId"];
        return Results.Content("[]", "application/json");
    }

    [HttpGet("/roomserver/rooms/bulk")]
    public async Task<IResult> RoomserverRoomsBulk(AppDbContext db) {
        var idParam = Request.Query["id"].FirstOrDefault();
        var nameParam = Request.Query["name"].FirstOrDefault();

        if (string.IsNullOrEmpty(idParam) && string.IsNullOrEmpty(nameParam))
            return Results.BadRequest("Either 'id' or 'name' query parameter is required");

        List<Room> results = new();

        if (!string.IsNullOrEmpty(idParam))
        {
            var ids = idParam.Split(',').Select(s => int.TryParse(s.Trim(), out var i) ? i : -1).Where(i => i != -1);
            results.AddRange(db.Rooms.Where(r => ids.Contains(r.RoomId)).ToList());
        }

        if (!string.IsNullOrEmpty(nameParam))
        {
            var names = nameParam.Split(',').Select(s => s.Trim().ToLower()).Where(s => !string.IsNullOrEmpty(s));
            results.AddRange(db.Rooms.Where(r => names.Any(n => r.Name.ToLower().Contains(n))).ToList());
        }

        var roomIds = results.Select(r => r.RoomId).ToList();

        if (roomIds.Count == 0)
            return Results.Json(new object[0]);

        var subRooms = db.SubRooms.Where(s => roomIds.Contains(s.RoomId)).ToList();
        var roles = db.RoomRoles.Where(r => roomIds.Contains(r.RoomId)).ToList();
        var loadScreens = db.LoadScreens.Where(l => roomIds.Contains(l.RoomId)).ToList();
        var promoImages = db.PromoImages.Where(p => roomIds.Contains(p.RoomId)).ToList();
        var promoExternalContents = db.PromoExternalContents.Where(p => roomIds.Contains(p.RoomId)).ToList();

        var response = results.Distinct().Select(r => new
        {
            r.RoomId,
            r.Name,
            r.Description,
            r.CreatorAccountId,
            r.ImageName,
            r.State,
            r.Accessibility,
            r.SupportsLevelVoting,
            r.IsRRO,
            r.IsDorm,
            r.CloningAllowed,
            r.SupportsVRLow,
            r.SupportsQuest2,
            r.SupportsMobile,
            r.SupportsScreens,
            r.SupportsWalkVR,
            r.SupportsTeleportVR,
            r.SupportsJuniors,
            r.MinLevel,
            r.WarningMask,
            r.CustomWarning,
            r.DisableMicAutoMute,
            r.DisableRoomComments,
            r.EncryptVoiceChat,
            r.CreatedAt,
            Stats = new { CheerCount = 0, FavoriteCount = 0, VisitorCount = 0, VisitCount = 0 },
            SubRooms = subRooms.Where(s => s.RoomId == r.RoomId).Select(s => new
            {
                s.SubRoomId,
                s.Name,
                s.DataBlob,
                s.IsSandbox,
                s.MaxPlayers,
                s.Accessibility,
                s.UnitySceneId,
                DataSavedAt = s.DataSavedAt
            }).ToList(),
            Roles = roles.Where(ro => ro.RoomId == r.RoomId).Select(ro => new
            {
                ro.AccountId,
                ro.Role,
                ro.InvitedRole
            }).ToList(),
            LoadScreens = loadScreens.Where(l => l.RoomId == r.RoomId).Select(l => new
            {
                l.ImageUrl,
                l.Tooltip,
                l.IsThumbnail
            }).ToList(),
            PromoImages = promoImages.Where(p => p.RoomId == r.RoomId).Select(p => new
            {
                p.ImageUrl,
                p.Tooltip,
                p.SortOrder
            }).ToList(),
            PromoExternalContent = promoExternalContents.Where(p => p.RoomId == r.RoomId).Select(p => new
            {
                p.Type,
                p.Url,
                p.Tooltip
            }).ToList(),
            Tags = new object[0]
        }).ToList();

        return Results.Json(response);
    }

    [HttpGet("/roomserver/rooms/hot")]
    public async Task<IResult> RoomserverRoomsHot(AppDbContext db) {
        var tagFilter = Request.Query["tag"].FirstOrDefault()?.ToLower();
        var allRooms = await db.Rooms.ToListAsync();

        if (!string.IsNullOrEmpty(tagFilter))
        {
            allRooms = allRooms.Where(r =>
            {
                var roomTags = TryDeserializeRoomTags(r.Tags);
                if (roomTags == null || roomTags.Length == 0) return false;
                return roomTags.Any(t => t.Tag.Equals(tagFilter, StringComparison.OrdinalIgnoreCase));
            }).ToList();
        }

        var hotRooms = allRooms.OrderByDescending(r => r.Id).ToList();
        var results = new List<object>();

        foreach (var room in hotRooms)
        {
            var subRooms = await db.SubRooms.Where(x => x.RoomId == room.RoomId).ToListAsync();
            var roles = await db.RoomRoles.Where(x => x.RoomId == room.RoomId).ToListAsync();
            var loadScreens = await db.LoadScreens.Where(x => x.RoomId == room.RoomId).ToListAsync();
            var promoImages = await db.PromoImages.Where(x => x.RoomId == room.RoomId).ToListAsync();
            var external = await db.PromoExternalContents.Where(x => x.RoomId == room.RoomId).ToListAsync();
            var tags = TryDeserializeTags(room.Tags) ?? new object[0];

            results.Add(new
            {
                room.RoomId,
                room.Name,
                room.Description,
                room.CreatorAccountId,
                room.ImageName,
                room.State,
                room.Accessibility,
                room.SupportsLevelVoting,
                room.IsRRO,
                room.IsDorm,
                room.CloningAllowed,
                room.SupportsVRLow,
                room.SupportsQuest2,
                room.SupportsMobile,
                room.SupportsScreens,
                room.SupportsWalkVR,
                room.SupportsTeleportVR,
                room.SupportsJuniors,
                room.MinLevel,
                room.WarningMask,
                room.CustomWarning,
                room.DisableMicAutoMute,
                room.DisableRoomComments,
                room.EncryptVoiceChat,
                room.CreatedAt,
                Stats = (object?)null,
                SubRooms = subRooms.Select(s => new { s.SubRoomId, s.RoomId, s.Name, s.DataBlob, s.IsSandbox, s.MaxPlayers, s.Accessibility, s.UnitySceneId, s.DataSavedAt }),
                Roles = roles.Select(r => new { r.AccountId, r.Role, r.InvitedRole }),
                LoadScreens = loadScreens.Select(ls => new { ls.ImageUrl, ls.Tooltip, ls.IsThumbnail }),
                PromoImages = promoImages.Select(pi => new { pi.ImageUrl, pi.Tooltip, pi.SortOrder }),
                PromoExternalContent = external.Select(ec => new { ec.Type, ec.Url, ec.Tooltip }),
                Tags = tags
            });
        }

        return Results.Json(new { Results = results, TotalResults = results.Count });
    }

    [HttpGet("/roomserver/roomsandplaylists/hot")]
    public async Task<IResult> RoomserverRoomsAndPlaylistsHot(AppDbContext db) {
        var allRooms = await db.Rooms.Where(r => !r.IsDorm).OrderByDescending(r => r.Id).ToListAsync();
        var results = new List<object>();

        foreach (var room in allRooms)
        {
            var subRooms = await db.SubRooms.Where(x => x.RoomId == room.RoomId).ToListAsync();
            var roles = await db.RoomRoles.Where(x => x.RoomId == room.RoomId).ToListAsync();
            var loadScreens = await db.LoadScreens.Where(x => x.RoomId == room.RoomId).ToListAsync();
            var promoImages = await db.PromoImages.Where(x => x.RoomId == room.RoomId).ToListAsync();
            var external = await db.PromoExternalContents.Where(x => x.RoomId == room.RoomId).ToListAsync();
            var tags = TryDeserializeTags(room.Tags) ?? new object[0];

            results.Add(new
            {
                room.RoomId,
                room.Name,
                room.Description,
                room.CreatorAccountId,
                room.ImageName,
                room.State,
                room.Accessibility,
                room.SupportsLevelVoting,
                room.IsRRO,
                room.IsDorm,
                room.CloningAllowed,
                room.SupportsVRLow,
                room.SupportsQuest2,
                room.SupportsMobile,
                room.SupportsScreens,
                room.SupportsWalkVR,
                room.SupportsTeleportVR,
                room.SupportsJuniors,
                room.MinLevel,
                room.WarningMask,
                room.CustomWarning,
                room.DisableMicAutoMute,
                room.DisableRoomComments,
                room.EncryptVoiceChat,
                room.CreatedAt,
                Stats = (object?)null,
                SubRooms = subRooms.Select(s => new { s.SubRoomId, s.RoomId, s.Name, s.DataBlob, s.IsSandbox, s.MaxPlayers, s.Accessibility, s.UnitySceneId, s.DataSavedAt }),
                Roles = roles.Select(r => new { r.AccountId, r.Role, r.InvitedRole }),
                LoadScreens = loadScreens.Select(ls => new { ls.ImageUrl, ls.Tooltip, ls.IsThumbnail }),
                PromoImages = promoImages.Select(pi => new { pi.ImageUrl, pi.Tooltip, pi.SortOrder }),
                PromoExternalContent = external.Select(ec => new { ec.Type, ec.Url, ec.Tooltip }),
                Tags = tags
            });
        }

        return Results.Json(new { Results = results, TotalResults = results.Count });
    }

    [HttpGet("/roomserver/rooms/{id}")]
    public async Task<IResult> RoomserverRoomsId(string id, AppDbContext db) {
        if (!int.TryParse(id, out var roomId))
            return Results.NotFound();

        var room = await db.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
        if (room == null)
            return Results.NotFound();

        var subRooms = await db.SubRooms.Where(x => x.RoomId == room.RoomId).ToListAsync();
        var roles = await db.RoomRoles.Where(x => x.RoomId == room.RoomId).ToListAsync();
        var loadScreens = await db.LoadScreens.Where(x => x.RoomId == room.RoomId).ToListAsync();
        var promoImages = await db.PromoImages.Where(x => x.RoomId == room.RoomId).ToListAsync();
        var external = await db.PromoExternalContents.Where(x => x.RoomId == room.RoomId).ToListAsync();
        var tags = TryDeserializeTags(room.Tags) ?? new object[0];

        return Results.Json(new
        {
            room.RoomId,
            room.Name,
            room.Description,
            room.CreatorAccountId,
            room.ImageName,
            room.State,
            room.Accessibility,
            room.SupportsLevelVoting,
            room.IsRRO,
            room.IsDorm,
            room.CloningAllowed,
            room.SupportsVRLow,
            room.SupportsQuest2,
            room.SupportsMobile,
            room.SupportsScreens,
            room.SupportsWalkVR,
            room.SupportsTeleportVR,
            room.SupportsJuniors,
            room.MinLevel,
            room.WarningMask,
            room.CustomWarning,
            room.DisableMicAutoMute,
            room.DisableRoomComments,
            room.EncryptVoiceChat,
            room.CreatedAt,
            Stats = new RoomStats { CheerCount = 0, FavoriteCount = 0, VisitorCount = 1, VisitCount = 1 },
            SubRooms = subRooms.Select(s => new { s.SubRoomId, s.RoomId, s.Name, s.DataBlob, s.IsSandbox, s.MaxPlayers, s.Accessibility, s.UnitySceneId, s.DataSavedAt }).ToList(),
            Roles = roles.Select(r => new { r.AccountId, r.Role, r.InvitedRole }).ToList(),
            LoadScreens = loadScreens.Select(l => new { l.ImageUrl, l.Tooltip, l.IsThumbnail }).ToList(),
            PromoImages = promoImages.Select(p => new { p.ImageUrl, p.Tooltip, p.SortOrder }).ToList(),
            PromoExternalContent = external.Select(e => new { e.Type, e.Url, e.Tooltip }).ToList(),
            Tags = tags
        });
    }

    [HttpGet("images/v2/named")]
    public async Task<IResult> NamedImages() {
        var json = System.IO.File.ReadAllText("JSON/namedimages.json");
        return Results.Content(json, "application/json");
    }

    [HttpPost("objectives/v1/updateobjective")]
    public async Task<IResult> UpdateObjective() {
        return Results.Ok();
    }

    [HttpPost("PlayerReporting/v1/hile")]
    public async Task<IResult> PlayerReportingHile() {
        return Results.Ok();
    }

    [HttpGet("storefronts/v3/giftdropstore/300")]
    public async Task<IResult> GiftDropStore300(StorefrontFillService storefrontService) {
        var storefronts = await storefrontService.GetStorefrontsAsync();
        var storefront = storefronts.FirstOrDefault(s => s.StorefrontType == 300 && s.Name == "rc_cafe_storefront");
        if (storefront == null)
        {
            var json = System.IO.File.ReadAllText("JSON/cafestorefront.json");
            return Results.Content(json, "application/json");
        }
        var storeItems = storefront.Items.Select(item => new
        {
            item.Id,
            item.StorefrontId,
            item.PurchasableItemId,
            item.Type,
            item.IsFeatured,
            item.NewUntil,
            GiftDrops = item.GiftDrops.Select(gd => new
            {
                gd.Id, gd.StorefrontItemId, gd.GiftDropId, gd.FriendlyName, gd.Tooltip,
                gd.ConsumableItemDesc, gd.AvatarItemDesc, gd.AvatarItemType,
                gd.EquipmentPrefabName, gd.EquipmentModificationGuid, gd.IsQuery,
                gd.Unique, gd.SubscribersOnly, gd.Level, gd.Rarity, gd.CurrencyType,
                gd.Currency, gd.Context, gd.ItemSetId, gd.ItemSetFriendlyName
            }).ToList(),
            Prices = item.Prices.Select(p => new { p.Id, p.StorefrontItemId, p.CurrencyType, p.Price }).ToList()
        }).ToList();
        return Results.Ok(new { storefront.Id, storefront.Name, storefront.StorefrontType, storefront.NextUpdate, StoreItems = storeItems });
    }

    [HttpGet("storefronts/v3/giftdropstore/2")]
    public async Task<IResult> GiftDropStore2(StorefrontFillService storefrontService) {
        var storefronts = await storefrontService.GetStorefrontsAsync();
        var storefront = storefronts.FirstOrDefault(s => s.StorefrontType == 2 && s.Name == "rec_center_store");
        if (storefront == null)
        {
            var json = System.IO.File.ReadAllText("JSON/storefront12.json");
            return Results.Content(json, "application/json");
        }
        var storeItems = storefront.Items.Select(item => new
        {
            item.Id,
            item.StorefrontId,
            item.PurchasableItemId,
            item.Type,
            item.IsFeatured,
            item.NewUntil,
            GiftDrops = item.GiftDrops.Select(gd => new
            {
                gd.Id, gd.StorefrontItemId, gd.GiftDropId, gd.FriendlyName, gd.Tooltip,
                gd.ConsumableItemDesc, gd.AvatarItemDesc, gd.AvatarItemType,
                gd.EquipmentPrefabName, gd.EquipmentModificationGuid, gd.IsQuery,
                gd.Unique, gd.SubscribersOnly, gd.Level, gd.Rarity, gd.CurrencyType,
                gd.Currency, gd.Context, gd.ItemSetId, gd.ItemSetFriendlyName
            }).ToList(),
            Prices = item.Prices.Select(p => new { p.Id, p.StorefrontItemId, p.CurrencyType, p.Price }).ToList()
        }).ToList();
        return Results.Ok(new { storefront.Id, storefront.Name, storefront.StorefrontType, storefront.NextUpdate, StoreItems = storeItems });
    }

    [HttpPost("storefronts/v2/buyItem")]
    [Authorize]
    public async Task<IResult> BuyItem(BuyItemRequest buyRequest, AppDbContext db, NotificationService notificationService) {
        try
        {
            int id = User.Identity.Name != null && int.TryParse(User.Identity.Name, out var parsedId) ? parsedId : -1;

            var storefrontItem = await db.StorefrontItems
                .Include(si => si.Prices)
                .Include(si => si.GiftDrops)
                .FirstOrDefaultAsync(si => si.PurchasableItemId == buyRequest.PurchasableItemId);

            if (storefrontItem == null)
                return Results.NotFound(new { error = "Item not found" });

            var price = storefrontItem.Prices.FirstOrDefault(p => p.CurrencyType == buyRequest.CurrencyType);
            if (price == null)
                return Results.BadRequest(new { error = "Currency type not available for this item" });

            var tokenBalance = await db.TokenBalances
                .FirstOrDefaultAsync(tb => tb.Id == id && tb.CurrencyType == buyRequest.CurrencyType && tb.BalanceType == -1);

            if (tokenBalance == null)
                return Results.BadRequest(new { error = "Account has no token balance" });

            if (tokenBalance.Balance < price.Price)
                return Results.BadRequest(new { error = "Insufficient balance" });

            tokenBalance.Balance -= price.Price;
            db.TokenBalances.Update(tokenBalance);
            await db.SaveChangesAsync();

            var receiverAccountId = id;
            if (buyRequest.Gift != null)
            {
                receiverAccountId = buyRequest.Gift.ToPlayerId;
            }

            var storedGifts = new List<ReceivedGift>();
            foreach (var giftDrop in storefrontItem.GiftDrops)
            {
                var receivedGift = new ReceivedGift
                {
                    ReceiverAccountId = receiverAccountId,
                    FromPlayerId = 1,
                    Message = buyRequest.Gift?.Message ?? "A gift for you <3",
                    ConsumableItemDesc = giftDrop.ConsumableItemDesc ?? string.Empty,
                    AvatarItemDesc = giftDrop.AvatarItemDesc ?? string.Empty,
                    AvatarItemType = giftDrop.AvatarItemType,
                    EquipmentPrefabName = giftDrop.EquipmentPrefabName ?? string.Empty,
                    EquipmentModificationGuid = giftDrop.EquipmentModificationGuid ?? string.Empty,
                    CurrencyType = giftDrop.CurrencyType,
                    Currency = giftDrop.Currency,
                    Xp = 0,
                    Level = giftDrop.Level,
                    Platform = -1,
                    PlatformsToSpawnOn = -1,
                    BalanceType = 0,
                    GiftContext = buyRequest.Gift?.GiftContext ?? giftDrop.Context,
                    GiftRarity = giftDrop.Rarity,
                    ReceivedAt = DateTime.UtcNow,
                    IsConsumed = false
                };
                db.ReceivedGifts.Add(receivedGift);
                storedGifts.Add(receivedGift);
            }
            await db.SaveChangesAsync();

            var giftData = storedGifts.Select(rg => new GiftData
            {
                Id = rg.Id, FromPlayerId = null,
                ConsumableItemDesc = rg.ConsumableItemDesc, AvatarItemDesc = rg.AvatarItemDesc,
                FriendlyName = rg.FriendlyName, AvatarItemType = rg.AvatarItemType,
                EquipmentPrefabName = rg.EquipmentPrefabName, EquipmentModificationGuid = rg.EquipmentModificationGuid,
                CurrencyType = rg.CurrencyType, Currency = rg.Currency, Xp = rg.Xp, Level = rg.Level,
                Platform = rg.Platform, PlatformsToSpawnOn = rg.PlatformsToSpawnOn, BalanceType = rg.BalanceType,
                GiftContext = rg.GiftContext, GiftRarity = rg.GiftRarity, Message = rg.Message
            }).ToList();

            var balanceUpdate = new BalanceUpdate { UpdateResponse = 0, Data = giftData };
            var response = new BuyItemResponse
            {
                BalanceUpdates = new List<BalanceUpdate> { balanceUpdate },
                Balance = tokenBalance.Balance, CurrencyType = buyRequest.CurrencyType,
                BalanceType = -1, Platform = -1
            };

            try
            {
                var purchaseNotification = notificationService.CreateNotification(
                    PushNotificationId.StorefrontBalancePurchase, id: storefrontItem.Id, toAccountId: id,
                    data: new Dictionary<string, object>
                    {
                        { "BalanceAddType", 1400 }, { "Delta", -price.Price },
                        { "Balance", tokenBalance.Balance }, { "CurrencyType", buyRequest.CurrencyType }
                    });
                await notificationService.SendNotificationToPlayer(id, purchaseNotification);
            }
            catch (Exception notifEx)
            {
                Console.WriteLine($"Error sending purchase notification: {notifEx.Message}");
            }

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error processing purchase: {ex.Message}");
        }
    }

    [HttpPost("avatar/v2/gifts/generate")]
    [Authorize]
    public async Task<IResult> AvatarGiftsGenerate(AppDbContext db) {
        try
        {
            int id = User.Identity.Name != null && int.TryParse(User.Identity.Name, out var parsedId) ? parsedId : -1;

            Request.EnableBuffering();
            Request.Body.Position = 0;
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            int giftContext = 0;
            bool isGameGift = false;
            string message = "";
            int xp = 0;

            if (!string.IsNullOrWhiteSpace(body))
            {
                foreach (var pair in body.Split('&'))
                {
                    var keyValue = pair.Split('=');
                    if (keyValue.Length == 2)
                    {
                        var key = Uri.UnescapeDataString(keyValue[0]);
                        var value = Uri.UnescapeDataString(keyValue[1]);

                        if (key == "GiftContext" && int.TryParse(value, out var context))
                            giftContext = context;
                        else if (key == "IsGameGift")
                            isGameGift = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                        else if (key == "Message")
                            message = value;
                        else if (key == "Xp" && int.TryParse(value, out var xpVal))
                            xp = xpVal;
                    }
                }
            }

            var earnableRewards = await db.EarnableRewards
                .Where(er => er.RewardContext == giftContext)
                .ToListAsync();

            var random = new Random();
            string avatarItemDesc = "";
            string friendlyName = "";
            int avatarItemType = 0;
            int currencyType = 0;
            int currency = 0;
            int giftRarity = 0;
            string consumableItemDesc = "";

            bool isEarnableItem = random.Next(0, 100) < 60;

            if (isEarnableItem && earnableRewards.Any())
            {
                var avatarRewards = earnableRewards.Where(er => !string.IsNullOrEmpty(er.AvatarItemDesc)).ToList();
                if (avatarRewards.Any())
                {
                    var ownedAvatarItems = await db.AvatarItems
                        .Where(ai => ai.OwnerAccountId == id)
                        .Select(ai => ai.AvatarItemDesc)
                        .ToListAsync();

                    var unownedRewards = avatarRewards.Where(ar => !ownedAvatarItems.Contains(ar.AvatarItemDesc)).ToList();
                    if (unownedRewards.Any())
                    {
                        var selectedReward = unownedRewards[random.Next(unownedRewards.Count)];
                        avatarItemDesc = selectedReward.AvatarItemDesc;
                        friendlyName = selectedReward.FriendlyName;
                        avatarItemType = selectedReward.AvatarItemType;
                        giftRarity = selectedReward.GiftRarity;
                    }
                    else { isEarnableItem = false; }
                }
                else { isEarnableItem = false; }
            }

            if (!isEarnableItem)
            {
                int[] tokenAmounts = { 10, 25, 50, 100, 250, 500 };
                currency = tokenAmounts[random.Next(tokenAmounts.Length)];
                currencyType = 2;
                giftRarity = 20;
            }

            var receivedGift = new ReceivedGift
            {
                ReceiverAccountId = id, FromPlayerId = 1, Message = message,
                ConsumableItemDesc = consumableItemDesc, AvatarItemDesc = avatarItemDesc,
                FriendlyName = friendlyName, AvatarItemType = avatarItemType,
                EquipmentPrefabName = "", EquipmentModificationGuid = "",
                CurrencyType = currencyType, Currency = currency, Xp = xp, Level = 0,
                Platform = -1, PlatformsToSpawnOn = -1, BalanceType = 0,
                GiftContext = giftContext, GiftRarity = giftRarity,
                ReceivedAt = DateTime.UtcNow, IsConsumed = false
            };

            db.ReceivedGifts.Add(receivedGift);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                Id = receivedGift.Id, FromPlayerId = 1,
                ConsumableItemDesc = consumableItemDesc, AvatarItemDesc = avatarItemDesc,
                FriendlyName = friendlyName, AvatarItemType = avatarItemType,
                EquipmentPrefabName = "", EquipmentModificationGuid = "",
                CurrencyType = currencyType, Currency = currency, Xp = xp, Level = 0,
                Platform = -1, PlatformsToSpawnOn = -1, BalanceType = 0,
                GiftContext = giftContext, GiftRarity = giftRarity, Message = message
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error generating gift: {ex.Message}");
        }
    }

    [HttpPost("avatar/v2/gifts/consume/")]
    [Authorize]
    public async Task<IResult> AvatarGiftsConsume(AppDbContext db) {
        try
        {
            int id = User.Identity.Name != null && int.TryParse(User.Identity.Name, out var parsedId) ? parsedId : -1;

            Request.EnableBuffering();
            Request.Body.Position = 0;
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            var giftId = 0;
            var unlockedLevel = 0;

            if (!string.IsNullOrWhiteSpace(body))
            {
                foreach (var pair in body.Split('&'))
                {
                    var keyValue = pair.Split('=');
                    if (keyValue.Length == 2)
                    {
                        if (keyValue[0] == "Id")
                            int.TryParse(Uri.UnescapeDataString(keyValue[1]), out giftId);
                        else if (keyValue[0] == "UnlockedLevel")
                            int.TryParse(Uri.UnescapeDataString(keyValue[1]), out unlockedLevel);
                    }
                }
            }

            if (giftId == 0)
                return Results.BadRequest(new { success = false, error = "Invalid gift ID" });

            var receivedGift = await db.ReceivedGifts
                .FirstOrDefaultAsync(rg => rg.Id == giftId && rg.ReceiverAccountId == id);
            if (receivedGift == null)
                return Results.NotFound(new { success = false, error = "Gift not found" });

            if (!string.IsNullOrEmpty(receivedGift.ConsumableItemDesc))
            {
                var existingConsumable = await db.ConsumableItems
                    .FirstOrDefaultAsync(c => c.OwnerAccountId == id && c.ConsumableItemDesc == receivedGift.ConsumableItemDesc);

                if (existingConsumable == null)
                {
                    db.ConsumableItems.Add(new ConsumableItem
                    {
                        OwnerAccountId = id,
                        Ids = new List<int> { giftId },
                        CreatedAts = new List<DateTime> { DateTime.UtcNow },
                        ConsumableItemDesc = receivedGift.ConsumableItemDesc,
                        Count = 1, InitialCount = 1, IsActive = false,
                        ActiveDurationMinutes = 0, IsTransferable = false
                    });
                }
                else
                {
                    existingConsumable.Ids.Add(giftId);
                    existingConsumable.CreatedAts.Add(DateTime.UtcNow);
                    existingConsumable.Count += 1;
                    db.ConsumableItems.Update(existingConsumable);
                }
            }

            if (!string.IsNullOrEmpty(receivedGift.AvatarItemDesc))
            {
                db.AvatarItems.Add(new AvatarItem
                {
                    OwnerAccountId = id,
                    AvatarItemDesc = receivedGift.AvatarItemDesc,
                    FriendlyName = receivedGift.FriendlyName ?? ""
                });
            }

            if (!string.IsNullOrEmpty(receivedGift.Currency.ToString()) && receivedGift.CurrencyType == 2)
            {
                var tokenBalance = await db.TokenBalances
                    .FirstOrDefaultAsync(tb => tb.Id == id && tb.CurrencyType == receivedGift.CurrencyType && tb.BalanceType == -1);

                if (tokenBalance == null)
                {
                    db.TokenBalances.Add(new TokenBalance
                    {
                        Id = id, CurrencyType = receivedGift.CurrencyType,
                        BalanceType = -1, Balance = receivedGift.Currency
                    });
                }
                else
                {
                    tokenBalance.Balance += receivedGift.Currency;
                    db.TokenBalances.Update(tokenBalance);
                }
            }

            receivedGift.IsConsumed = true;
            receivedGift.ConsumedAt = DateTime.UtcNow;
            db.ReceivedGifts.Update(receivedGift);
            await db.SaveChangesAsync();

            return Results.Ok(RecNetResult.Ok());
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error consuming gift: {ex.Message}");
        }
    }

    [HttpGet("rooms/v1/filters")]
    public async Task<IResult> RoomsFilters() {
        var json = System.IO.File.ReadAllText("JSON/roomfilters.json");
        return Results.Content(json, "application/json");
    }

    [HttpGet("/roomserver/rooms/{id}/interactionby/me")]
    public async Task<IResult> RoomInteractionByMe(string id) {
        return Results.Content("{\"Cheered\":false,\"Favorited\":false}", "application/json");
    }

    [HttpPost("images/v4/uploadsaved")]
    public async Task<IResult> ImageUploadSaved() {
        try
        {
            var form = await HttpContext.Request.ReadFormAsync();
            var file = form.Files.FirstOrDefault();
            if (file == null)
                return Results.BadRequest(new { error = "No file found in request" });

            var imageId = Guid.NewGuid().ToString("N");
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var validExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp" };
            if (string.IsNullOrEmpty(extension) || !validExtensions.Contains(extension))
                extension = ".png";

            var savedFileName = imageId + extension;
            var filePath = Path.Combine(ImagesDir, savedFileName);

            using (var fileStream = System.IO.File.Create(filePath))
                await file.CopyToAsync(fileStream);

            return Results.Ok(new { ImageName = savedFileName });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error uploading image: {ex.Message}");
        }
    }

    [HttpGet("config/v1/backtrace")]
    public IResult BacktraceConfig() {
        return Results.Ok(new
        {
            ReportBudget = 125,
            FilterType = 0,
            SampleRate = 0.025,
            LogLineCount = 50,
            CaptureNativeCrashes = 1,
            AMRThresholdMS = 0,
            MessageCount = 1000,
            MessageRegex = "^Cannot set the parent of the GameObject .* while its new parent|^\\\\>\\\\x2010x\\\\:\\\\x20|\\\\'LabelTheme\\\\' contains missing PaletteTheme reference on",
            VersionRegex = ".*"
        });
    }


    private static object[]? TryDeserializeTags(string json)
    {
        try
        {
            var tags = System.Text.Json.JsonSerializer.Deserialize<RoomTag[]>(json);
            if (tags != null)
                return tags.Cast<object>().ToArray();
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static RoomTag[]? TryDeserializeRoomTags(string json)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<RoomTag[]>(json);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<List<int>> ParseFormIds(HttpRequest httpRequest)
    {
        var ids = new List<int>();
        
        if (httpRequest.ContentLength.HasValue && httpRequest.ContentLength > 0)
        {
            httpRequest.EnableBuffering();
            using var reader = new StreamReader(httpRequest.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            
            if (!string.IsNullOrWhiteSpace(body))
            {
                foreach (var pair in body.Split('&'))
                {
                    var keyValue = pair.Split('=');
                    if (keyValue.Length == 2 && keyValue[0] == "Ids")
                    {
                        var idString = Uri.UnescapeDataString(keyValue[1]);
                        foreach (var id in idString.Split(','))
                            if (int.TryParse(id, out var parsedId))
                                ids.Add(parsedId);
                        break;
                    }
                }
            }
            httpRequest.Body.Position = 0;
        }
        
        return ids;
    }
    
    

    private static readonly string ImagesDir;
    static APIController()
    {
        var dir = "Images";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        ImagesDir = dir;
    }
}
