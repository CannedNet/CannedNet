using CannedNet;
using CannedNet.Data;
using Microsoft.EntityFrameworkCore;
using CannedNet.Services;
using CannedNet.Services.Infrastructure;

namespace CannedNet.Services.Controllers;

public class AuthController
{
    public WebApplicationBuilder Initialize(string[]? args = null) => ServiceExtensions.CreateRecNetBuilder(args);

    public void MapEndpoints(WebApplication app)
    {
        var jwtService = app.Services.GetRequiredService<JwtTokenService>();

        app.MapGet("/eac/challenge", () =>
        {
            var file = File.ReadAllText("JSON/eacchallenge.txt");
            return Results.Content(file, "text/plain");
        });
        
        app.MapGet("/cachedlogin/forplatformid/{platform}/{id}", async (string platform, string id, AppDbContext db) =>
        {
            var platformType = int.Parse(platform);
            var logins = await db.CachedLogins
                .Where(c => c.Platform == (PlatformType)platformType && c.PlatformID == id)
                .ToListAsync();
    
            return Results.Json(logins.Any() ? logins : new List<object>());
        });
        
        app.MapPost("/connect/token", async (HttpRequest httpRequest, AppDbContext db) =>
        {
            string accountId = "";
            string platformId = "";
            string platform = "";

            if (httpRequest.ContentLength.HasValue && httpRequest.ContentLength > 0)
            {
                try
                {
                    httpRequest.EnableBuffering();
                    using var reader = new StreamReader(httpRequest.Body, leaveOpen: true);
                    var body = await reader.ReadToEndAsync();
                    
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        foreach (var pair in body.Split('&'))
                        {
                            var keyValue = pair.Split('=');
                            if (keyValue.Length == 2)
                            {
                                var key = Uri.UnescapeDataString(keyValue[0]);
                                var value = Uri.UnescapeDataString(keyValue[1]);

                                if (key == "account_id")
                                    accountId = value;
                                else if (key == "platform_id")
                                    platformId = value;
                            }
                        }
                    }
                    httpRequest.Body.Position = 0;
                }
                catch { }
            }

            var accessToken = jwtService.GenerateToken(accountId, platformId, platform);
            
            if (!string.IsNullOrEmpty(accountId) && int.TryParse(accountId, out var id))
            {
                var roomInstance = await db.RoomInstances.FirstOrDefaultAsync(r => r.OwnerAccountId == id);
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
        });
        
        app.MapGet("/role/developer/{id}", async (HttpRequest request, AppDbContext db, string id) =>
        {
            // TODO: implement
            return Results.Ok(RecNetResult.Ok());
        });
        
        app.MapPost("/api/accounts/v1/forplatformids", async (HttpRequest request, AppDbContext db) =>
        {
            request.EnableBuffering();
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();
            
            var ids = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(body))
            {
                foreach (var pair in body.Split('&'))
                {
                    var keyValue = pair.Split('=');
                    if (keyValue.Length == 2 && keyValue[0] == "Ids")
                    {
                        var idString = Uri.UnescapeDataString(keyValue[1]);
                        ids = idString.Split(',').ToList();
                        break;
                    }
                }
            }
            
            var results = new List<object>();
            foreach (var platformId in ids)
            {
                var cachedLogin = await db.CachedLogins.FirstOrDefaultAsync(c => c.PlatformID == platformId);
                if (cachedLogin != null)
                {
                    results.Add(new { accountId = cachedLogin.AccountId, platformId = platformId });
                }
            }
            
            return Results.Json(results);
        });
    }
}
