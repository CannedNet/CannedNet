using CannedNet.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace CannedNet.Services.Controllers;

[ApiController, Route("auth")]
public class AuthController : ControllerBase
{
    [HttpGet("eac/challenge")]
    public async Task<IResult> EacChallenge()
    {
        string file = await System.IO.File.ReadAllTextAsync("JSON/eacchallenge.txt");
        return Results.Content(file, "text/plain");
    }

    [HttpGet("cachedlogin/forplatformid/{platform}/{id}")]
    public async Task<IResult> CachedLoginForPlatformId(string platform, string id, AppDbContext db)
    {
        int platformType = int.Parse(platform);
        List<CachedLogin> logins = await db.CachedLogins
            .Where(c => c.Platform == (PlatformType)platformType && c.PlatformID == id)
            .ToListAsync();

        return Results.Json(logins.Any() ? logins : new List<object>());
    }

    [HttpPost("connect/token")]
    public async Task<IResult> ConnectToken(AppDbContext db, JwtTokenService jwtService, ConfigService config)
    {
        string grantType = "";
        string accountId = "";
        string platformId = "";
        string platform = "";
        int platformInt = 0;
        string password = "";
        string username = "";
        List<string> adminRoles = [];

        if (HttpContext.Request.ContentLength is > 0)
        {
            try
            {
                HttpContext.Request.EnableBuffering();
                using StreamReader reader = new(HttpContext.Request.Body, leaveOpen: true);
                string body = await reader.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(body))
                {
                    foreach (string pair in body.Split('&'))
                    {
                        string[] keyValue = pair.Split('=');
                        if (keyValue.Length != 2) continue;
                        string key = Uri.UnescapeDataString(keyValue[0]);
                        string value = Uri.UnescapeDataString(keyValue[1]);

                        switch (key)
                        {
                            case "grant_type":
                                grantType = value;
                                break;
                            case "account_id":
                                accountId = value;
                                break;
                            case "platform_id":
                                platformId = value;
                                if (config.Config.WhitelistOn)
                                {
                                    if (!config.Config.WhitelistedPlatformIds.Contains(platformId))
                                        return Results.Unauthorized();
                                }
                                break;
                            case "platform" when int.TryParse(value, out var p):
                                platformInt = p;
                                platform = ((PlatformType)p).ToString();
                                break;
                            case "username":
                                username = value;
                                break;
                            case "password":
                                password = value;
                                break;
                        }
                    }
                }
                HttpContext.Request.Body.Position = 0;
            }
            catch { }
        }

        if (grantType == "create_account")
        {
            if (!config.Config.EnableAccountCreation)
            {
                return Results.Ok(RecNetResult.Err("Account creation is disabled"));
            }

            int newId = new Random().Next(10000, 99999);
            Account account = new()
            {
                AccountId = newId,
                ProfileImage = "DefaultProfileImage.jpg",
                IsJunior = false,
                Platforms = 0,
                PersonalPronouns = 0,
                IdentityFlags = 0,
                Username = $"Player{newId}",
                DisplayName = $"Player{newId}",
                CreatedAt = DateTime.UtcNow
            };

            db.Accounts.Add(account);

            if (!string.IsNullOrEmpty(platformId))
            {
                db.CachedLogins.Add(new CachedLogin
                {
                    AccountId = newId,
                    Platform = (PlatformType)platformInt,
                    PlatformID = platformId,
                    LastLoginTime = DateTime.UtcNow,
                    RequirePassword = false
                });
            }

            await db.SaveChangesAsync();

            int maxRoomId = await db.Rooms.MaxAsync(r => (int?)r.RoomId) ?? 0;
            int maxId = await db.Rooms.MaxAsync(r => (int?)r.Id) ?? 0;
            int dormRoomId = maxRoomId + 1;
            Room dormRoom = new Room
            {
                Id = maxId + 1,
                RoomId = dormRoomId,
                Name = "DormRoom",
                Description = "Your personal room",
                CreatorAccountId = newId,
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

            SubRoom dormSubRoom = new SubRoom
            {
                RoomId = dormRoomId,
                SubRoomId = 1,
                Name = "DormRoom",
                DataBlob = "",
                IsSandbox = false,
                MaxPlayers = 4,
                Accessibility = 0,
                UnitySceneId = "76d98498-60a1-430c-ab76-b54a29b7a163",
                DataSavedAt = DateTime.UtcNow
            };
            db.SubRooms.Add(dormSubRoom);

            await db.SaveChangesAsync();

            accountId = newId.ToString();
        }
        else if (grantType == "password")
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Results.Ok(RecNetResult.Err("Username and password are required"));

            var account = await db.Accounts.FirstOrDefaultAsync(a => a.Username == username);
            if (account == null || string.IsNullOrEmpty(account.Password))
                return Results.Ok(RecNetResult.Err("Invalid username or password"));

            var hasher = new PasswordHasher<Account>();
            var result = hasher.VerifyHashedPassword(account, account.Password, password);
            if (result == PasswordVerificationResult.Failed)
                return Results.Ok(RecNetResult.Err("Invalid username or password"));

            accountId = account.AccountId?.ToString() ?? "";
            if (account.AccountId != null && config.Config.AdminAccountIds.Contains(account.AccountId.Value.ToString()))
                adminRoles.Add("developer");
            platformId = accountId;
        }

        string accessToken = jwtService.GenerateToken(accountId, platformId, config, platform, grantType == "password" ? adminRoles : null, amr: grantType == "create_account" ? "create_account" : "cached_login");

        if (!string.IsNullOrEmpty(accountId) && int.TryParse(accountId, out var id))
        {
            RoomInstance? roomInstance = await db.RoomInstances.FirstOrDefaultAsync(r => r.OwnerAccountId == id);
            if (roomInstance != null)
            {
                db.RoomInstances.Remove(roomInstance);
                await db.SaveChangesAsync();
            }
        }

        return Results.Json(new
        {
            access_token = accessToken,
            expires_in = 3600,
            token_type = "Bearer",
            refresh_token = Guid.NewGuid().ToString("N").ToUpper() + "-1",
            scope = "offline_access profile rn rn.accounts rn.accounts.gc rn.api rn.chat rn.clubs rn.commerce rn.match.read rn.match.write rn.notify rn.rooms rn.storage",
            key = "8oQ+e+WQaOBPbEcakhqs3dwZZdOmmyDUmJSD9u4AHMY="
        });
    }

    [HttpPost("api/accounts/v1/forplatformids")]
    public async Task<IResult> AccountsForPlatformIds(AppDbContext db)
    {

        HttpContext.Request.EnableBuffering();
        HttpContext.Request.Body.Position = 0;
        using var reader = new StreamReader(HttpContext.Request.Body);
        string body = await reader.ReadToEndAsync();

        List<string> ids = [];

        if (!string.IsNullOrWhiteSpace(body))
        {
            foreach (string pair in body.Split('&'))
            {
                string[] keyValue = pair.Split('=');
                if (keyValue.Length != 2 || keyValue[0] != "Ids") continue;

                string idString = Uri.UnescapeDataString(keyValue[1]);
                ids = idString.Split(',').ToList();
                break;
            }
        }

        List<object> results = [];
        foreach (string platformId in ids)
        {
            CachedLogin? cachedLogin = await db.CachedLogins.FirstOrDefaultAsync(c => c.PlatformID == platformId);
            if (cachedLogin != null)
            {
                results.Add(new { accountId = cachedLogin.AccountId, platformId = platformId });
            }
        }

        return Results.Json(results);
    }

    [HttpGet("role/developer/{id}")]
    public async Task<IResult> GetDeveloperRole(string id)
    {
        return Results.Ok(RecNetResult.Ok());
    }
}
