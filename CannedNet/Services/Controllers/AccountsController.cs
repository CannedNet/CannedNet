using CannedNet.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace CannedNet.Services.Controllers;

//TODO: remove duplicate code(most of it is checking if the user is authenticated, ASP.NET has stuff for this)
[ApiController, Route("account")]
public class AccountsController : ControllerBase
{
    [HttpGet("me")]
    public async Task<IResult> Me(HttpRequest request, AppDbContext db, JwtTokenService jwtService) {
        string authHeader = request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Results.Unauthorized();

        string token = authHeader.Substring("Bearer ".Length);
        string? accountId = jwtService.ValidateAndGetAccountId(token);

        if (string.IsNullOrEmpty(accountId) || !int.TryParse(accountId.AsSpan(), out int id))
            return Results.Unauthorized();

        Account? account = await db.Accounts.FindAsync(id);
        if (account == null)
            return Results.NotFound();

        SelfAccount selfAccount = new SelfAccount {
            AccountId = id,
            ProfileImage = account.ProfileImage ?? "hdqeamlcmatc6qzoi2ybgf0ddijjcf.jpg",
            IsJunior = account.IsJunior,
            Platforms = account.Platforms ?? 0,
            PersonalPronouns = account.PersonalPronouns ?? 0,
            IdentityFlags = account.IdentityFlags ?? 0,
            Username = account.Username ?? $"Player{id}",
            DisplayName = account.DisplayName ?? $"Player{id}",
            CreatedAt = account.CreatedAt,
            Email = null,
            Phone = null,
            JuniorState = null,
            Birthday = null,
            ParentAccountId = null,
            AvailableUsernameChanges = 1
        };

        return Results.Ok(selfAccount);
    }

    [HttpGet("bulk")]
    public async Task<IResult> Bulk(HttpRequest request, AppDbContext db) {
        StringValues ids = request.Query["id"];
        List<int> accountIds = [];

        foreach (string? id in ids) {
            if (int.TryParse(id, out int accountId)) {
                accountIds.Add(accountId);
            }
        }

        List<Account> accounts = await db.Accounts
            .Where(a => accountIds.Contains(a.AccountId.Value))
            .ToListAsync();

        List<Account> result = accounts.Select(a => new Account {
            AccountId = a.AccountId,
            ProfileImage = a.ProfileImage ?? "DefaultProfileImage.jpg",
            IsJunior = a.IsJunior,
            Platforms = a.Platforms ?? 0,
            PersonalPronouns = a.PersonalPronouns ?? 0,
            IdentityFlags = a.IdentityFlags ?? 0,
            Username = a.Username ?? $"Player{a.AccountId}",
            DisplayName = a.DisplayName ?? $"Player{a.AccountId}",
            CreatedAt = a.CreatedAt
        }).ToList();

        return Results.Json(result);
    }

    [HttpGet("{id}")]
    public async Task<IResult> Id(HttpRequest request, string id, AppDbContext db) {
        if (!int.TryParse(id, out int accountId))
            return Results.BadRequest();

        Account? account = await db.Accounts.FindAsync(accountId);
        if (account == null)
            return Results.NotFound();

        Account result = new() {
            AccountId = account.AccountId,
            ProfileImage = account.ProfileImage ?? "DefaultProfileImage.jpg",
            IsJunior = account.IsJunior,
            Platforms = account.Platforms ?? 0,
            PersonalPronouns = account.PersonalPronouns ?? 0,
            IdentityFlags = account.IdentityFlags ?? 0,
            Username = account.Username ?? $"Player{account.AccountId}",
            DisplayName = account.DisplayName ?? $"Player{account.AccountId}",
            CreatedAt = account.CreatedAt
        };

        return Results.Json(result);
    }

    [HttpPost("create")]
    public async Task<IResult> Create(HttpRequest httpRequest, AppDbContext db) {
        int platform = 0;
        string platformId = "";

        if (httpRequest.ContentLength is > 0) {
            try {
                string contentType = httpRequest.ContentType ?? "";
                httpRequest.EnableBuffering();

                using StreamReader reader = new(httpRequest.Body, leaveOpen: true);
                string body = await reader.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(body) && contentType.Contains("application/x-www-form-urlencoded")) {
                    foreach (string pair in body.Split('&')) {
                        string[] keyValue = pair.Split('=');
                        if (keyValue.Length != 2) continue;
                        string key = Uri.UnescapeDataString(keyValue[0]);
                        string value = Uri.UnescapeDataString(keyValue[1]);

                        switch (key) {
                            case "platform" when int.TryParse(value, out var parsedPlatform):
                                platform = parsedPlatform;
                                break;
                            case "platformId":
                                platformId = value;
                                break;
                        }
                    }
                }

                httpRequest.Body.Position = 0;
            }
            catch { }
        }

        int accountId = new Random().Next(10000, 99999);
        Account account = new() {
            AccountId = accountId,
            ProfileImage = "DefaultProfileImage.jpg",
            IsJunior = false,
            Platforms = 0,
            PersonalPronouns = 0,
            IdentityFlags = 0,
            Username = $"Player{accountId}",
            DisplayName = $"Player{accountId}",
            CreatedAt = DateTime.UtcNow
        };

        db.Accounts.Add(account);

        if (!string.IsNullOrEmpty(platformId)) {
            db.CachedLogins.Add(new CachedLogin {
                AccountId = accountId,
                Platform = (PlatformType)platform,
                PlatformID = platformId,
                LastLoginTime = DateTime.UtcNow,
                RequirePassword = false
            });
        }

        await db.SaveChangesAsync();

        // create players dorm room
        int maxRoomId = await db.Rooms.MaxAsync(r => (int?)r.RoomId) ?? 0;
        int maxId = await db.Rooms.MaxAsync(r => (int?)r.Id) ?? 0;
        int dormRoomId = maxRoomId + 1;
        //int dormId = maxId + 1;
        Room dormRoom = new Room {
            Id = maxId + 1,
            RoomId = dormRoomId,
            Name = "DormRoom",
            Description = "Your personal room",
            CreatorAccountId = accountId,
            ImageName = "",
            State = 0,
            Accessibility = 0,
            SupportsLevelVoting = false,
            IsRRO = false,
            IsDorm = true,
            CloningAllowed = false,
            SupportsVRLow = true,
            SupportsQuest2 = true,
            SupportsMobile = true,
            SupportsScreens = true,
            SupportsWalkVR = true,
            SupportsTeleportVR = true,
            SupportsJuniors = true,
            MinLevel = 0,
            WarningMask = 0,
            CustomWarning = null,
            DisableMicAutoMute = false,
            DisableRoomComments = false,
            EncryptVoiceChat = false,
            CreatedAt = DateTime.UtcNow,
            Tags = "[]"
        };
        db.Rooms.Add(dormRoom);

        // Create a sub room for the dorm
        SubRoom dormSubRoom = new SubRoom {
            RoomId = dormRoomId,
            SubRoomId = 1,
            Name = "DormRoom",
            DataBlob = "",
            IsSandbox = false,
            MaxPlayers = 4,
            Accessibility = 0,
            UnitySceneId = "76d98498-60a1-430c-ab76-b54a29b7a163", // Dorm scene ID
            DataSavedAt = DateTime.UtcNow
        };
        db.SubRooms.Add(dormSubRoom);

        await db.SaveChangesAsync();
        return Results.Ok(RecNetResult.Ok(account));
    }

    [HttpPut("me/displayname")]
    public async Task<IResult> PutMeDisplayName(HttpRequest request, AppDbContext db, JwtTokenService jwtService) {
        string authHeader = request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Results.Unauthorized();

        string token = authHeader.Substring("Bearer ".Length);
        string? accountId = jwtService.ValidateAndGetAccountId(token);

        if (string.IsNullOrEmpty(accountId) || !int.TryParse(accountId.AsSpan(), out var id))
            return Results.Unauthorized();

        string newDisplayName = "";

        if (request.ContentLength is > 0) {
            try {
                request.EnableBuffering();
                using StreamReader reader = new(request.Body, leaveOpen: true);
                string body = await reader.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(body)) {
                    foreach (string pair in body.Split('&')) {
                        string[] keyValue = pair.Split('=');
                        if (keyValue.Length != 2) continue;
                        string key = Uri.UnescapeDataString(keyValue[0]);
                        string value = Uri.UnescapeDataString(keyValue[1]);

                        if (key == "displayName")
                            newDisplayName = value;
                    }
                }

                request.Body.Position = 0;
            }
            catch { }
        }

        Account? account = await db.Accounts.FindAsync(id);
        if (account == null)
            return Results.NotFound();

        account.DisplayName = newDisplayName;
        await db.SaveChangesAsync();

        return Results.Ok(RecNetResult.Ok());
    }

    [HttpPut("me/username")]
    public async Task<IResult> PutMeUsername(HttpRequest request, AppDbContext db, JwtTokenService jwtService) {
        string authHeader = request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(authHeader) ||
            !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Results.Unauthorized();

        string token = authHeader.Substring("Bearer ".Length);
        string? accountId = jwtService.ValidateAndGetAccountId(token);

        if (string.IsNullOrEmpty(accountId) || !int.TryParse(accountId.AsSpan(), out int id))
            return Results.Unauthorized();

        string newAccountName = "";

        if (request.ContentLength is > 0) {
            try {
                request.EnableBuffering();
                using StreamReader reader = new(request.Body, leaveOpen: true);
                string body = await reader.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(body)) {
                    foreach (string pair in body.Split('&')) {
                        string[] keyValue = pair.Split('=');
                        if (keyValue.Length != 2) continue;
                        string key = Uri.UnescapeDataString(keyValue[0]);
                        string value = Uri.UnescapeDataString(keyValue[1]);

                        if (key == "username")
                            newAccountName = value;
                    }
                }

                request.Body.Position = 0;
            }
            catch { }
        }

        Account? account = await db.Accounts.FindAsync(id);
        if (account == null)
            return Results.NotFound();

        account.Username = newAccountName;
        await db.SaveChangesAsync();

        return Results.Ok(RecNetResult.Ok());
    }

    [HttpGet("{id}/bio")]
    public async Task<IResult> GetBio(string id, AppDbContext db) {
        if (!int.TryParse(id, out int accountId))
            return Results.BadRequest();

        PlayerBio? bio = await db.PlayerBios.FirstOrDefaultAsync(b => b.accountId == accountId);

        return Results.Json(bio == null
            ? new { accountId = accountId, bio = "" }
            : new { accountId = bio.accountId, bio = bio.bio });
    }

    [HttpPut("me/bio")]
    public async Task<IResult> PutMeBio(HttpRequest request, AppDbContext db, JwtTokenService jwtService) {
        string authHeader = request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(authHeader) ||
            !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Results.Unauthorized();

        string token = authHeader.Substring("Bearer ".Length);
        string? accountId = jwtService.ValidateAndGetAccountId(token);

        if (string.IsNullOrEmpty(accountId) || !int.TryParse(accountId.AsSpan(), out int id))
            return Results.Unauthorized();

        string newBio = "";

        if (request.ContentLength is > 0) {
            try {
                request.EnableBuffering();
                using StreamReader reader = new StreamReader(request.Body, leaveOpen: true);
                string body = await reader.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(body)) {
                    foreach (string pair in body.Split('&')) {
                        string[] keyValue = pair.Split('=');
                        if (keyValue.Length != 2) continue;
                        string key = Uri.UnescapeDataString(keyValue[0]);
                        string value = Uri.UnescapeDataString(keyValue[1]);

                        if (key == "bio")
                            newBio = Uri.UnescapeDataString(value);
                    }
                }

                request.Body.Position = 0;
            }
            catch { }
        }

        PlayerBio? bio = await db.PlayerBios.FirstOrDefaultAsync(b => b.accountId == id);

        if (bio == null) {
            bio = new PlayerBio {
                accountId = id,
                bio = newBio
            };
            db.PlayerBios.Add(bio);
        }
        else {
            bio.bio = newBio;
            db.PlayerBios.Update(bio);
        }

        await db.SaveChangesAsync();

        return Results.Json(new { success = true });
    }

    [HttpPut("me/profileimage")]
    public async Task<IResult> PutMeProfileImage(HttpRequest request, AppDbContext db, JwtTokenService jwtService) {
        string authHeader = request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(authHeader) ||
            !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Results.Unauthorized();

        string token = authHeader.Substring("Bearer ".Length);
        string? accountId = jwtService.ValidateAndGetAccountId(token);

        if (string.IsNullOrEmpty(accountId) || !int.TryParse(accountId.AsSpan(), out int id))
            return Results.Unauthorized();

        string newProfileImage = "";

        if (request.ContentLength is > 0) {
            try {
                request.EnableBuffering();
                using StreamReader reader = new(request.Body, leaveOpen: true);
                string body = await reader.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(body)) {
                    foreach (var pair in body.Split('&')) {
                        string[] keyValue = pair.Split('=');
                        if (keyValue.Length != 2) continue;
                        string key = Uri.UnescapeDataString(keyValue[0]);
                        string value = Uri.UnescapeDataString(keyValue[1]);

                        if (key == "imageName")
                            newProfileImage = value;
                    }
                }

                request.Body.Position = 0;
            }
            catch { }
        }

        Account? account = await db.Accounts.FindAsync(id);
        if (account == null)
            return Results.NotFound();

        account.ProfileImage = newProfileImage;
        await db.SaveChangesAsync();

        return Results.Ok(RecNetResult.Ok());
    }

    [HttpGet("parentalcontrol/me")]
    public async Task<IResult> ParentalControl(HttpRequest request, JwtTokenService jwtService) {
        string authHeader = request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(authHeader) ||
            !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Results.Unauthorized();

        string token = authHeader.Substring("Bearer ".Length);
        string? accountId = jwtService.ValidateAndGetAccountId(token);

        if (string.IsNullOrEmpty(accountId) || !int.TryParse(accountId.AsSpan(), out _))
            return Results.Unauthorized();

        return Results.Content($"{{\"accountId\":{accountId},\"disallowInAppPurchases\":false}}", "application/json");
    }
}