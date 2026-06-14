using CannedNet.Data;
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
    public async Task<IResult> ConnectToken(AppDbContext db, JwtTokenService jwtService)
    {
        string accountId = "";
        string platformId = "";
        string platform = "";

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
                            case "account_id":
                                accountId = value;
                                break;
                            case "platform_id":
                                platformId = value;
                                break;
                        }
                    }
                }
                HttpContext.Request.Body.Position = 0;
            }
            catch { }
        }

        string accessToken = jwtService.GenerateToken(accountId, platformId, platform);

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
