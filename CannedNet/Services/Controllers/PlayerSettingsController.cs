using CannedNet.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CannedNet.Services.Controllers;

[ApiController, Route("playersettings")]
public class PlayerSettingsController : ControllerBase
{
    [HttpGet("playersettings")]
    [Authorize]
    public async Task<ActionResult> GetPlayerSettings(AppDbContext db)
    {
        if (!int.TryParse(User.Identity?.Name, out var id))
            return Unauthorized();

        var settings = await db.PlayerSettings
            .Where(s => s.PlayerId == id)
            .ToListAsync();

        return Ok(settings);
    }

    [HttpPut("playersettings")]
    [Authorize]
    public async Task<IResult> PutPlayerSettings(AppDbContext db)
    {
        if (!int.TryParse(User.Identity?.Name, out var id))
            return Results.Unauthorized();

        var settings = new List<PlayerSetting>();

        if (HttpContext.Request.ContentType?.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) == true)
        {
            var form = await HttpContext.Request.ReadFormAsync();
            var key = form["key"].FirstOrDefault();
            var value = form["value"].FirstOrDefault();
            if (!string.IsNullOrEmpty(key))
                settings.Add(new PlayerSetting { Key = key, Value = value ?? "" });
        }
        else
        {
            HttpContext.Request.EnableBuffering();
            HttpContext.Request.Body.Position = 0;
            using var reader = new StreamReader(HttpContext.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            HttpContext.Request.Body.Position = 0;

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
    }
}
