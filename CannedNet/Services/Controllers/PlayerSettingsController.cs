using CannedNet.Data;
using CannedNet.Services.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CannedNet.Services.Controllers;

public class PlayerSettingsController
{
    public WebApplicationBuilder Initialize(string[]? args = null) => ServiceExtensions.CreateRecNetBuilder(args);

    public void MapEndpoints(WebApplication app)
    {
        var jwtService = app.Services.GetRequiredService<JwtTokenService>();
        var notificationService = app.Services.GetRequiredService<NotificationService>();
        
        app.MapGet("/playersettings" +
                   "", async (HttpRequest request, AppDbContext db) =>
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
        });
        
        app.MapPut("/playersettings", async (HttpRequest request, AppDbContext db) =>
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

            var settings = new List<PlayerSetting>();
            
            if (request.ContentType?.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) == true)
            {
                var form = await request.ReadFormAsync();
                var key = form["key"].FirstOrDefault();
                var value = form["value"].FirstOrDefault();
                if (!string.IsNullOrEmpty(key))
                    settings.Add(new PlayerSetting { Key = key, Value = value ?? "" });
            }
            else
            {
                request.EnableBuffering();
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0;
                
                Console.WriteLine($"Settings request body: {body}");
                
                if (body.TrimStart().StartsWith("["))
                {
                    settings = System.Text.Json.JsonSerializer.Deserialize<List<PlayerSetting>>(body) ?? [];
                }
                else
                {
                    var single = System.Text.Json.JsonSerializer.Deserialize<PlayerSetting>(body);
                    if (single != null) settings.Add(single);
                }
            }
            
            settings = settings.Where(s => !string.IsNullOrEmpty(s.Key)).ToList();

            if (!settings.Any())
                return Results.Ok();
            
            db.PlayerSettings.RemoveRange(db.PlayerSettings.Where(s => s.PlayerId == id));
            
            foreach (var setting in settings)
            {
                setting.PlayerId = id;
                setting.Key = setting.Key ?? "";
                setting.Value = setting.Value ?? "";
                db.PlayerSettings.Add(setting);
            }
            
            await db.SaveChangesAsync();
            return Results.Ok();
        });
    }
}
