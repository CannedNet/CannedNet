using System.Text.Json;
using CannedNet.Data;
using CannedNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CannedNet.Services.Controllers;

[ApiController, Route("admin/api")]
[Authorize(Roles = "developer,admin")]
public class AdminController : ControllerBase
{
    private const string JsonDir = "JSON";

    [HttpGet("accounts/search")]
    public async Task<IResult> SearchAccounts([FromQuery] string q, AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Results.Ok(new List<Account>());

        var accounts = await db.Accounts
            .Where(a => a.Username != null && a.Username.ToLower().Contains(q.ToLower())
                || a.DisplayName != null && a.DisplayName.ToLower().Contains(q.ToLower())
                || a.AccountId.ToString() == q)
            .Take(50)
            .ToListAsync();

        return Results.Ok(accounts);
    }

    [HttpGet("accounts/{id:int}")]
    public async Task<IResult> GetAccount(int id, AppDbContext db)
    {
        var account = await db.Accounts.FindAsync(id);
        if (account == null)
            return Results.NotFound();

        var bio = await db.PlayerBios.FirstOrDefaultAsync(b => b.accountId == id);
        var progression = await db.PlayerProgressions.FirstOrDefaultAsync(p => p.PlayerId == id);

        return Results.Ok(new
        {
            Account = account,
            Bio = bio?.bio ?? "",
            Progression = progression,
            AvatarItems = await db.AvatarItems.Where(a => a.OwnerAccountId == id).ToListAsync(),
            Settings = await db.PlayerSettings.Where(s => s.PlayerId == id).ToListAsync(),
            TokenBalances = await db.TokenBalances.Where(t => t.Id == id).ToListAsync()
        });
    }

    [HttpPut("accounts/{id:int}")]
    public async Task<IResult> UpdateAccount(int id, AppDbContext db)
    {
        var account = await db.Accounts.FindAsync(id);
        if (account == null)
            return Results.NotFound();

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(body);

        if (data.TryGetProperty("username", out var username))
            account.Username = username.GetString();
        if (data.TryGetProperty("displayName", out var displayName))
            account.DisplayName = displayName.GetString();
        if (data.TryGetProperty("profileImage", out var profileImage))
            account.ProfileImage = profileImage.GetString();
        if (data.TryGetProperty("isJunior", out var isJunior))
            account.IsJunior = isJunior.GetBoolean();
        if (data.TryGetProperty("personalPronouns", out var personalPronouns))
            account.PersonalPronouns = personalPronouns.GetInt32();
        if (data.TryGetProperty("identityFlags", out var identityFlags))
            account.IdentityFlags = identityFlags.GetInt32();

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    [HttpDelete("accounts/{id:int}")]
    public async Task<IResult> DeleteAccount(int id, AppDbContext db)
    {
        var account = await db.Accounts.FindAsync(id);
        if (account == null)
            return Results.NotFound();

        db.Accounts.Remove(account);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    [HttpGet("rooms/search")]
    public async Task<IResult> SearchRooms([FromQuery] string q, AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Results.Ok(new List<Room>());

        var rooms = await db.Rooms
            .Where(r => r.Name.ToLower().Contains(q.ToLower())
                || r.Description.ToLower().Contains(q.ToLower())
                || r.RoomId.ToString() == q
                || r.Id.ToString() == q)
            .Take(50)
            .ToListAsync();

        return Results.Ok(rooms);
    }

    [HttpGet("rooms/{id:int}")]
    public async Task<IResult> GetRoom(int id, AppDbContext db)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.RoomId == id);
        if (room == null)
            return Results.NotFound();

        var subRooms = await db.SubRooms.Where(s => s.RoomId == id).ToListAsync();
        var loadScreens = await db.LoadScreens.Where(l => l.RoomId == id).ToListAsync();
        var promoImages = await db.PromoImages.Where(p => p.RoomId == id).ToListAsync();
        var roles = await db.RoomRoles.Where(r => r.RoomId == id).ToListAsync();

        return Results.Ok(new
        {
            Room = room,
            SubRooms = subRooms,
            LoadScreens = loadScreens,
            PromoImages = promoImages,
            Roles = roles
        });
    }

    [HttpPut("rooms/{id:int}")]
    public async Task<IResult> UpdateRoom(int id, AppDbContext db)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.RoomId == id);
        if (room == null)
            return Results.NotFound();

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(body);

        if (data.TryGetProperty("name", out var name))
            room.Name = name.GetString() ?? room.Name;
        if (data.TryGetProperty("description", out var desc))
            room.Description = desc.GetString() ?? room.Description;
        if (data.TryGetProperty("imageName", out var imageName))
            room.ImageName = imageName.GetString() ?? room.ImageName;
        if (data.TryGetProperty("state", out var state))
            room.State = state.GetInt32();
        if (data.TryGetProperty("accessibility", out var accessibility))
            room.Accessibility = accessibility.GetInt32();
        if (data.TryGetProperty("isDorm", out var isDorm))
            room.IsDorm = isDorm.GetBoolean();
        if (data.TryGetProperty("isRRO", out var isRRO))
            room.IsRRO = isRRO.GetBoolean();
        if (data.TryGetProperty("cloningAllowed", out var cloningAllowed))
            room.CloningAllowed = cloningAllowed.GetBoolean();
        if (data.TryGetProperty("minLevel", out var minLevel))
            room.MinLevel = minLevel.GetInt32();
        if (data.TryGetProperty("warningMask", out var warningMask))
            room.WarningMask = warningMask.GetInt32();
        if (data.TryGetProperty("customWarning", out var customWarning))
            room.CustomWarning = customWarning.GetString();
        if (data.TryGetProperty("supportsVRLow", out var supportsVRLow))
            room.SupportsVRLow = supportsVRLow.GetBoolean();
        if (data.TryGetProperty("supportsQuest2", out var supportsQuest2))
            room.SupportsQuest2 = supportsQuest2.GetBoolean();
        if (data.TryGetProperty("supportsMobile", out var supportsMobile))
            room.SupportsMobile = supportsMobile.GetBoolean();
        if (data.TryGetProperty("supportsScreens", out var supportsScreens))
            room.SupportsScreens = supportsScreens.GetBoolean();
        if (data.TryGetProperty("supportsWalkVR", out var supportsWalkVR))
            room.SupportsWalkVR = supportsWalkVR.GetBoolean();
        if (data.TryGetProperty("supportsTeleportVR", out var supportsTeleportVR))
            room.SupportsTeleportVR = supportsTeleportVR.GetBoolean();
        if (data.TryGetProperty("supportsJuniors", out var supportsJuniors))
            room.SupportsJuniors = supportsJuniors.GetBoolean();
        if (data.TryGetProperty("tags", out var tags))
            room.Tags = tags.GetString() ?? room.Tags;

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    [HttpDelete("rooms/{id:int}")]
    public async Task<IResult> DeleteRoom(int id, AppDbContext db)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.RoomId == id);
        if (room == null)
            return Results.NotFound();

        db.SubRooms.RemoveRange(db.SubRooms.Where(s => s.RoomId == id));
        db.LoadScreens.RemoveRange(db.LoadScreens.Where(l => l.RoomId == id));
        db.PromoImages.RemoveRange(db.PromoImages.Where(p => p.RoomId == id));
        db.RoomRoles.RemoveRange(db.RoomRoles.Where(r => r.RoomId == id));
        db.Rooms.Remove(room);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    [HttpGet("config/gameconfigs")]
    public async Task<IResult> GetGameConfigs()
    {
        var json = await System.IO.File.ReadAllTextAsync(Path.Combine(JsonDir, "gameconfigs.json"));
        return Results.Content(json, "application/json");
    }

    [HttpPut("config/gameconfigs")]
    public async Task<IResult> SaveGameConfigs()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        await System.IO.File.WriteAllTextAsync(Path.Combine(JsonDir, "gameconfigs.json"), body);
        return Results.Ok(new { success = true });
    }

    [HttpGet("config/communityboard")]
    public async Task<IResult> GetCommunityBoard()
    {
        var json = await System.IO.File.ReadAllTextAsync(Path.Combine(JsonDir, "communityboard.json"));
        return Results.Content(json, "application/json");
    }

    [HttpPut("config/communityboard")]
    public async Task<IResult> SaveCommunityBoard()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        await System.IO.File.WriteAllTextAsync(Path.Combine(JsonDir, "communityboard.json"), body);
        return Results.Ok(new { success = true });
    }

    [HttpGet("config/configv2")]
    public async Task<IResult> GetConfigV2()
    {
        var json = await System.IO.File.ReadAllTextAsync(Path.Combine(JsonDir, "configv2.json"));
        return Results.Content(json, "application/json");
    }

    [HttpPut("config/configv2")]
    public async Task<IResult> SaveConfigV2()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        await System.IO.File.WriteAllTextAsync(Path.Combine(JsonDir, "configv2.json"), body);
        return Results.Ok(new { success = true });
    }

    [HttpGet("storefronts")]
    public async Task<IResult> GetStorefronts(AppDbContext db)
    {
        var storefronts = await db.Storefronts
            .Include(s => s.Items)
            .ThenInclude(i => i.GiftDrops)
            .Include(s => s.Items)
            .ThenInclude(i => i.Prices)
            .ToListAsync();
        return Results.Ok(storefronts);
    }

    [HttpGet("storefronts/{id:int}")]
    public async Task<IResult> GetStorefront(int id, AppDbContext db)
    {
        var storefront = await db.Storefronts
            .Include(s => s.Items)
            .ThenInclude(i => i.GiftDrops)
            .Include(s => s.Items)
            .ThenInclude(i => i.Prices)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (storefront == null)
            return Results.NotFound();

        return Results.Ok(storefront);
    }

    [HttpPost("storefronts/{storefrontId:int}/items")]
    public async Task<IResult> CreateStorefrontItem(int storefrontId, AppDbContext db)
    {
        var storefront = await db.Storefronts.FindAsync(storefrontId);
        if (storefront == null)
            return Results.NotFound();

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(body);

        var item = new StorefrontItem
        {
            StorefrontId = storefrontId,
            PurchasableItemId = data.TryGetProperty("purchasableItemId", out var pid) ? pid.GetInt32() : 0,
            Type = data.TryGetProperty("type", out var type) ? type.GetInt32() : 0,
            IsFeatured = data.TryGetProperty("isFeatured", out var featured) && featured.GetBoolean(),
            NewUntil = data.TryGetProperty("newUntil", out var newUntil) && newUntil.ValueKind == JsonValueKind.String
                ? DateTime.Parse(newUntil.GetString()!)
                : null
        };

        db.StorefrontItems.Add(item);
        await db.SaveChangesAsync();

        // create default price if provided
        if (data.TryGetProperty("price", out var priceEl) && data.TryGetProperty("currencyType", out var currencyEl))
        {
            db.StorefrontPrices.Add(new StorefrontPrice
            {
                StorefrontItemId = item.Id,
                CurrencyType = currencyEl.GetInt32(),
                Price = priceEl.GetInt32()
            });
            await db.SaveChangesAsync();
        }

        return Results.Ok(item);
    }

    [HttpPut("storefronts/items/{itemId:int}")]
    public async Task<IResult> UpdateStorefrontItem(int itemId, AppDbContext db)
    {
        var item = await db.StorefrontItems
            .Include(i => i.Prices)
            .Include(i => i.GiftDrops)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null)
            return Results.NotFound();

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(body);

        if (data.TryGetProperty("purchasableItemId", out var pid))
            item.PurchasableItemId = pid.GetInt32();
        if (data.TryGetProperty("type", out var type))
            item.Type = type.GetInt32();
        if (data.TryGetProperty("isFeatured", out var featured))
            item.IsFeatured = featured.GetBoolean();
        if (data.TryGetProperty("newUntil", out var newUntil))
            item.NewUntil = newUntil.ValueKind == JsonValueKind.String ? DateTime.Parse(newUntil.GetString()!) : null;

        // update prices
        if (data.TryGetProperty("prices", out var prices))
        {
            db.StorefrontPrices.RemoveRange(item.Prices);
            foreach (var p in prices.EnumerateArray())
            {
                db.StorefrontPrices.Add(new StorefrontPrice
                {
                    StorefrontItemId = item.Id,
                    CurrencyType = p.GetProperty("currencyType").GetInt32(),
                    Price = p.GetProperty("price").GetInt32()
                });
            }
        }

        await db.SaveChangesAsync();
        return Results.Ok(item);
    }

    [HttpDelete("storefronts/items/{itemId:int}")]
    public async Task<IResult> DeleteStorefrontItem(int itemId, AppDbContext db)
    {
        var item = await db.StorefrontItems
            .Include(i => i.Prices)
            .Include(i => i.GiftDrops)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null)
            return Results.NotFound();

        db.StorefrontPrices.RemoveRange(item.Prices);
        db.GiftDrops.RemoveRange(item.GiftDrops);
        db.StorefrontItems.Remove(item);
        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }
}
