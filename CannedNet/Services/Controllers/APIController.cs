using CannedNet;
using CannedNet.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using CannedNet.Hubs;
using CannedNet.Models;
using Microsoft.AspNetCore.SignalR;
using CannedNet.Services;
using CannedNet.Services.Infrastructure;

namespace CannedNet.Services.Controllers;

public class APIController
{
    public WebApplicationBuilder Initialize(string[]? args = null) => ServiceExtensions.CreateRecNetBuilder(args);

    public void MapEndpoints(WebApplication app)
    {
        var jwtService = app.Services.GetRequiredService<JwtTokenService>();
        var storefrontService = app.Services.GetRequiredService<StorefrontFillService>();
        var notificationService = app.Services.GetRequiredService<NotificationService>();

        app.MapGet("/api/config/v1/amplitude", () => Results.Ok(new
        {
            AmplitudeKey = "a",
            StatSigKey = "a",
            RudderStackKey = "a",
            UseRudderStack = false
        }));

        app.MapGet("/api/config/v1/azurespeech", () => Results.Ok(new
        {
            Key = "dce8de5b297747d9b5bddcc7f19e8c5b",
            Region = "eastus",
            Enabled = false
        }));

        app.MapPost("/api/gamesight/event", async (HttpRequest request) =>
        {
            string? eventType = null;
            string? userId = null;
            string? identifiers = null;

            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync();
                var eventData = form["EventData"].ToString();

                if (!string.IsNullOrWhiteSpace(eventData))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(eventData);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("type", out var typeProp))
                            eventType = typeProp.GetString();

                        if (root.TryGetProperty("user_id", out var userIdProp))
                            userId = userIdProp.GetString();

                        if (root.TryGetProperty("transaction_id", out var transactionProp))
                            identifiers = transactionProp.GetString();
                    }
                    catch (JsonException)
                    {
                    }
                }
            }

            var createdAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

            return Results.Json(new
            {
                @event = new
                {
                    created_at = createdAt,
                    identifiers = identifiers ?? "",
                    type = string.IsNullOrWhiteSpace(eventType) ? "game_launch" : eventType,
                    user_id = string.IsNullOrWhiteSpace(userId) ? "0" : userId
                }
            });
        });

        app.MapGet("/api/config/v2", () => Results.Content(File.ReadAllText("JSON/configv2.json"), "application/json"));
        
        app.MapGet("/api/versioncheck/v4", () => Results.Ok(new
        {
            VersionStatus = 0,
            UpdateNotificationStage = 0,
            IsVersionIslanded = false,
            IsCrossPlayDisabled = false
        }));
        
        app.MapGet("/api/gameconfigs/v1/all", () => Results.Content(File.ReadAllText("JSON/gameconfigs.json"), "application/json"));

        app.MapGet("/api/relationships/v2/get", () => Results.Content("[]", "application/json"));
        app.MapGet("/api/messages/v2/get", () => Results.Content("[]", "application/json"));

        app.MapGet("/api/playerReputation/v1/{id}", (string id) => 
            Results.Content($"{{\"AccountId\":{id},\"Noteriety\":0,\"CheerGeneral\":0,\"CheerHelpful\":0,\"CheerCreative\":0,\"CheerGreatHost\":0,\"CheerSportsman\":0,\"CheerCredit\":20,\"SelectedCheer\":null}}", "application/json"));

        app.MapGet("/api/players/v1/progression/{id}", (string id) => 
            Results.Content($"{{\"PlayerId\":{id},\"Level\":1,\"XP\":0}}", "application/json"));

        app.MapPost("/api/playerReputation/v2/bulk", async (HttpRequest httpRequest, AppDbContext db) =>
        {
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

            // TODO: implement real endpoint from grabbing from db
            var json = File.ReadAllText("JSON/bulkprogression.json");
            return Results.Content(json, "application/json");
        });

        app.MapPost("/api/players/v2/progression/bulk", async (HttpRequest httpRequest, AppDbContext db) =>
        {
            var ids = await ParseFormIds(httpRequest);
            
            if (!ids.Any())
                return Results.Json(new List<PlayerProgressionBulkResponse>());
            
            var progressions = await db.PlayerProgressions
                .Where(p => ids.Contains(p.PlayerId))
                .Select(p => new PlayerProgressionBulkResponse { PlayerId = p.PlayerId, Level = p.Level, Xp = p.Xp })
                .ToListAsync();
            
            return Results.Json(progressions);
        });

        app.MapPost("/api/v1/progression/bulk", async (HttpRequest httpRequest, AppDbContext db) =>
        {
            var ids = await ParseFormIds(httpRequest);
            
            if (!ids.Any())
                return Results.Json(new List<PlayerProgressionBulkResponse>());
            
            var progressions = await db.PlayerProgressions
                .Where(p => ids.Contains(p.PlayerId))
                .Select(p => new PlayerProgressionBulkResponse { PlayerId = p.PlayerId, Level = p.Level, Xp = p.Xp })
                .ToListAsync();
            
            return Results.Json(progressions);
        });
        
        app.MapGet("/api/avatar/v4/items", async (HttpRequest request, AppDbContext db) =>
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
            
            var ownedItems = await db.AvatarItems
                .Where(i => i.OwnerAccountId == id)
                .ToListAsync();

            var defaultItems = new List<object>
            {
                new { AvatarItemType = 0, AvatarItemDesc = "5d13a7a2-8213-40e6-90a6-efdd76a3fdcb,,,", PlatformMask = -1, FriendlyName = "Flowing Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "1d27b674-f9e2-4ffc-9d8c-a58a1be06457,,,", PlatformMask = -1, FriendlyName = "Afro Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "d84c0ff9-8fbe-4ed8-abf3-7996e81888ab,,,", PlatformMask = -1, FriendlyName = "Large Afro Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "e5b83dfc-b2e1-4dcb-a4ab-9d3a4c8a34ae,,,", PlatformMask = -1, FriendlyName = "Long Wavy Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "7dd6f7b0-7ba0-429f-a04f-e32d3a79ee61,,,", PlatformMask = -1, FriendlyName = "Short Wavy Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "eb9611c6-bb50-41a2-93e9-7f959815a846,,,", PlatformMask = -1, FriendlyName = "Dreads Long Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "1fd69ef8-0b74-4962-af5a-67f0bf0358f2,,,", PlatformMask = -1, FriendlyName = "Ponytail Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "a12f724f-4a73-4ab8-aad4-6bfc662b4dd6,,,", PlatformMask = -1, FriendlyName = "Undercut Long Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "0753d7a4-8247-4fca-a6fc-359c26086140,,,", PlatformMask = -1, FriendlyName = "Fonzie Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "77d3c585-4928-4471-a425-89036efe7299,,,", PlatformMask = -1, FriendlyName = "Spiky Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "92302d9d-c527-418c-ac5d-1fa869727505,,,", PlatformMask = -1, FriendlyName = "Part Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "f9dd08f8-16d3-4c39-af4f-89f7bb6e80d3,,,", PlatformMask = -1, FriendlyName = "Undercut Short Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "b148cb1e-df81-442f-aea6-ab1727aad00e,,,", PlatformMask = -1, FriendlyName = "Chunky Afro Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "e36bcd98-7e85-43fa-89f8-57e4ec33823a,,,", PlatformMask = -1, FriendlyName = "Bob with Bangs Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "880a3cc0-7407-4b61-b759-f9dd890fe9e5,,,", PlatformMask = -1, FriendlyName = "Bob Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "21599b51-c50f-43d8-ac5f-62c30cd02ca5,,,", PlatformMask = -1, FriendlyName = "Lori Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "193a3bf9-abc0-4d78-8d63-92046908b1c5,,,", PlatformMask = -1, FriendlyName = "Emo Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "79b90274-6eec-4664-acfb-4a123334661e,,,", PlatformMask = -1, FriendlyName = "Pig Tails Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "da4e7b34-2095-4a9e-801e-4f409039e0dd,,,", PlatformMask = -1, FriendlyName = "Buzz Cut Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "9d9fadb6-97eb-480e-a224-4e0179082071,,,", PlatformMask = -1, FriendlyName = "Meatball Buns Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "d8280c0c-d803-4513-be10-a0ba96d8821e,,,", PlatformMask = -1, FriendlyName = "Flowhawk Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "e286863c-2967-4d00-b837-b49487b9484a,,,", PlatformMask = -1, FriendlyName = "Fauxhawk Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "2cb4f372-3372-4583-8b57-c4e3988e3c28,,,", PlatformMask = -1, FriendlyName = "Punky Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "06306723-ca20-4aa6-b7b3-917113f41ac3,,,", PlatformMask = -1, FriendlyName = "Cat-Eye Glasses (Red)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "c70005d5-6276-4a98-acb3-6a77bc19379a,,,", PlatformMask = -1, FriendlyName = "Glasses (Teal)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "8d10cc78-6b00-45f3-affb-205e9cc5b03f,,,", PlatformMask = -1, FriendlyName = "Beard (Close)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "cc96f8a5-bc5b-4f89-83b7-ecd53905ada7,,,", PlatformMask = -1, FriendlyName = "Beard (Thick)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "c6c08eb5-381a-4193-9722-80da95d62abe,,,", PlatformMask = -1, FriendlyName = "Business Tie (Black)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "4d507dfa-4a99-4ac0-8537-229e9dc0eb4a,,,", PlatformMask = -1, FriendlyName = "Rec Room Tank Top (Orange)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "d0a9262f-5504-46a7-bb10-7507503db58e,,,", PlatformMask = -1, FriendlyName = "Rec Room Shirt (Crew Neck, White)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "d0a9262f-5504-46a7-bb10-7507503db58e,95e4cc30-cb68-473d-a395-feadf5b51512,0440f08f-ef1d-49d8-942b-523056e8bb45,", PlatformMask = -1, FriendlyName = "Rec Room T-Shirt (Crew Neck, Orange)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "2e59d8d0-91a0-4449-bfdc-a5d663fd9343,,,", PlatformMask = -1, FriendlyName = "Collared Shirt (Plaid, Blue)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "7b857a8c-92ad-4028-a2c2-b3c20cdab5f2,,,", PlatformMask = -1, FriendlyName = "T-Shirt", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "8aa79563-ace1-4ba7-ad0c-f3210a78142f,,,", PlatformMask = -1, FriendlyName = "Rec Room Shirt (V-Neck, White)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "8aa79563-ace1-4ba7-ad0c-f3210a78142f,95e4cc30-cb68-473d-a395-feadf5b51512,05f0ee6e-c824-470e-9178-5ed576c6fe0c,", PlatformMask = -1, FriendlyName = "Rec Room T-Shirt (V-Neck, Orange)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "21caa68e-c3fa-474c-af5e-af1e742b7a60,,,", PlatformMask = -1, FriendlyName = "Tennis Skirt (Blue)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "21caa68e-c3fa-474c-af5e-af1e742b7a60,c5deba2a-6e35-4b13-8e94-8ba5457f39df,b75ef67d-00c3-4ac1-9b72-212032460294,", PlatformMask = -1, FriendlyName = "Tennis Skirt (Yellow)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "21caa68e-c3fa-474c-af5e-af1e742b7a60,758752bd-db2f-43d2-b580-55b3e1efffd5,b75ef67d-00c3-4ac1-9b72-212032460294,", PlatformMask = -1, FriendlyName = "Tennis Skirt (Red)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "2296ed0d-df56-4d46-b33a-aae9230a47fc,,,", PlatformMask = -1, FriendlyName = "Zipper Dress (Yellow)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "ecc1dbe6-ca06-4564-b2a6-30956194d1e9,,,", PlatformMask = -1, FriendlyName = "Wristbands (White)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "71921831-ba6f-408b-a00e-2fd97663636f,,,", PlatformMask = -1, FriendlyName = "Wrist Tape (White)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "2e59d8d0-91a0-4449-bfdc-a5d663fd9343,55901f12-d5b5-4fa8-b4c8-e479689ee39d,f600037d-c9c0-43fa-b45b-02f456f9dd5f,", PlatformMask = -1, FriendlyName = "Collared Shirt (Denim)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "2e59d8d0-91a0-4449-bfdc-a5d663fd9343,bf82f2f6-9af8-431e-a296-0890dea48ba7,d015cae7-a905-49e4-8823-6dec069689a6,", PlatformMask = -1, FriendlyName = "Collared Shirt (Argyle)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "2e59d8d0-91a0-4449-bfdc-a5d663fd9343,EfdMcnfHt0mr0PQ_maaYOg,DRJcNhkqvkKFEaZpOguR6w,", PlatformMask = -1, FriendlyName = "Collared Shirt (Flowers, Green)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "7b857a8c-92ad-4028-a2c2-b3c20cdab5f2,6d703981-2734-4c45-8983-cdd5f328902f,a0271cd0-e172-4d3f-aa2f-9806f21a82d2,", PlatformMask = -1, FriendlyName = "Tank Top (Camo)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "7b857a8c-92ad-4028-a2c2-b3c20cdab5f2,5c4a2b35-0e1c-44de-8c3a-96d4a6458b1b,9c03f381-7357-4d0f-8cda-8737d4c43d25,", PlatformMask = -1, FriendlyName = "Tank Top (Rainbow)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "7b857a8c-92ad-4028-a2c2-b3c20cdab5f2,51ef8d39-2b94-4f9e-9620-07b6b0a913a5,d2a692e6-e1a9-4cfe-8154-10b52be7f8c8,", PlatformMask = -1, FriendlyName = "Jersey (Orange)", Tooltip = "", Rarity = 10 },
                new { AvatarItemType = 0, AvatarItemDesc = "7b857a8c-92ad-4028-a2c2-b3c20cdab5f2,ad61c418-6d77-4a99-8ac5-9f10f5a3d42f,b292eb4b-07e3-4a48-99b5-3c6587a1e02e,", PlatformMask = -1, FriendlyName = "Tank Top (Dots)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "7b857a8c-92ad-4028-a2c2-b3c20cdab5f2,48abd952-214f-48b2-a8f1-1146f6f69aa2,b78008e8-abbd-4ece-be34-9a911f721fcc,", PlatformMask = -1, FriendlyName = "Tank Top (Zebra)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "14ef6b00-debf-4a85-9755-b4d37df496d3,8377ab96-c908-457f-9fee-b784c9a759f3,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Baseball Cap (Red)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "14ef6b00-debf-4a85-9755-b4d37df496d3,dee70c38-7a99-4c2b-9181-665f1bf75aca,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Baseball Cap (Blue)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "40528de7-38a3-4a7c-8f93-6d3bfa5573f2,7d8e55fe-3c34-4b4b-9753-0021f6cc6454,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Headband (Cream)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "40528de7-38a3-4a7c-8f93-6d3bfa5573f2,1b1d08f2-12ca-43dd-a44f-ea2820b919b4,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Headband (Black)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "40528de7-38a3-4a7c-8f93-6d3bfa5573f2,51ef8d39-2b94-4f9e-9620-07b6b0a913a5,018a5c07-e956-457d-a540-a5e2cd68da09,", PlatformMask = -1, FriendlyName = "Headband (Orange, White)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "40528de7-38a3-4a7c-8f93-6d3bfa5573f2,cbe29e9f-f2ac-47fb-97e1-8bad16abb89d,018a5c07-e956-457d-a540-a5e2cd68da09,", PlatformMask = -1, FriendlyName = "Headband (Pink, White)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "40528de7-38a3-4a7c-8f93-6d3bfa5573f2,dee70c38-7a99-4c2b-9181-665f1bf75aca,018a5c07-e956-457d-a540-a5e2cd68da09,", PlatformMask = -1, FriendlyName = "Headband (Blue, White)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "40528de7-38a3-4a7c-8f93-6d3bfa5573f2,6dd95046-acf8-42fe-ab78-80a334096a9d,56a92c8d-af53-413e-929e-4a9a3cfad780,", PlatformMask = -1, FriendlyName = "Headband (Red, White, Blue)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "ecc1dbe6-ca06-4564-b2a6-30956194d1e9,dee70c38-7a99-4c2b-9181-665f1bf75aca,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Wristbands (Blue)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "ecc1dbe6-ca06-4564-b2a6-30956194d1e9,1b1d08f2-12ca-43dd-a44f-ea2820b919b4,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Wristbands (Black)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "ecc1dbe6-ca06-4564-b2a6-30956194d1e9,51ef8d39-2b94-4f9e-9620-07b6b0a913a5,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Wristbands (Orange)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "ecc1dbe6-ca06-4564-b2a6-30956194d1e9,cbe29e9f-f2ac-47fb-97e1-8bad16abb89d,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Wristbands (Pink)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "ecc1dbe6-ca06-4564-b2a6-30956194d1e9,8377ab96-c908-457f-9fee-b784c9a759f3,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Wristbands (Red)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "fcfcaf63-deb4-45f7-b711-c051c9ea45cb,,,", PlatformMask = -1, FriendlyName = "Top Bun Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "de0ac50d-2adb-4114-bd2e-68953b13d706,,,", PlatformMask = -1, FriendlyName = "Blazer (Blue, White)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "de0ac50d-2adb-4114-bd2e-68953b13d706,6f2e74bf-1e95-463d-97db-d5d1a53b2c28,be2b9293-1d3c-4b1c-b4c5-fad3ab16cf54,", PlatformMask = -1, FriendlyName = "Blazer (Black, White)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "de0ac50d-2adb-4114-bd2e-68953b13d706,9374bf66-2ee5-493b-8439-efce4b201904,be2b9293-1d3c-4b1c-b4c5-fad3ab16cf54,", PlatformMask = -1, FriendlyName = "Blazer (Grey, Black)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "de0ac50d-2adb-4114-bd2e-68953b13d706,272fe8eb-5061-4729-a7a8-414ff667a82f,be2b9293-1d3c-4b1c-b4c5-fad3ab16cf54,", PlatformMask = -1, FriendlyName = "Blazer (Grey, White)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "de0ac50d-2adb-4114-bd2e-68953b13d706,0ffad843-d6c9-425a-8686-7217009c867e,be2b9293-1d3c-4b1c-b4c5-fad3ab16cf54,", PlatformMask = -1, FriendlyName = "Blazer (Green, Black)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "9c8fc7f0-8f99-4aad-a34f-8d979f6ae352,e0397982-c2c2-4733-9a40-46e18675b5af,dafa658e-753b-46cb-bd85-85c1de5e6ea7,", PlatformMask = -1, FriendlyName = "Button Top (Orange)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "9c8fc7f0-8f99-4aad-a34f-8d979f6ae352,,,", PlatformMask = -1, FriendlyName = "Button Top (Pink)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "9c8fc7f0-8f99-4aad-a34f-8d979f6ae352,49f5864f-9d40-497c-88c8-e87f64d41d74,dafa658e-753b-46cb-bd85-85c1de5e6ea7,", PlatformMask = -1, FriendlyName = "Button Top (Tan)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "9c8fc7f0-8f99-4aad-a34f-8d979f6ae352,c5deba2a-6e35-4b13-8e94-8ba5457f39df,dafa658e-753b-46cb-bd85-85c1de5e6ea7,", PlatformMask = -1, FriendlyName = "Button Top (Yellow)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "6d815b35-6f68-4ed4-817d-70f141e1a571,f750de46-3758-4f7d-9709-0a84b1027009,2c8924aa-68f8-4912-9759-18992f72f08a,", PlatformMask = -1, FriendlyName = "Collared Dress (Blue)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "6d815b35-6f68-4ed4-817d-70f141e1a571,d66aa400-aa5a-4539-a25d-5f8ce94dc281,2c8924aa-68f8-4912-9759-18992f72f08a,", PlatformMask = -1, FriendlyName = "Collared Dress (Green)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "6d815b35-6f68-4ed4-817d-70f141e1a571,6564acf1-4d70-4f92-92ac-08e2b76dbb6b,2c8924aa-68f8-4912-9759-18992f72f08a,", PlatformMask = -1, FriendlyName = "Collared Dress (Purple)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "6d815b35-6f68-4ed4-817d-70f141e1a571,,,", PlatformMask = -1, FriendlyName = "Collared Dress (Red)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "241506f6-bf88-4b46-b5fe-513a225421f4,,,", PlatformMask = -1, FriendlyName = "Half Up Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "6b9e022c-0b68-48fd-8eca-da8573c18900,d6edbc00-3c1d-4f49-8412-3ef8c7c5f4c2,cf119781-5bd9-4b85-9a0b-12e82e988c23,", PlatformMask = -1, FriendlyName = "Long Scarf (Blue)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "2296ed0d-df56-4d46-b33a-aae9230a47fc,6d703981-2734-4c45-8983-cdd5f328902f,cfabdefe-0890-436e-b2a3-b5c712e22955,", PlatformMask = -1, FriendlyName = "Zipper Dress (Green)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "2296ed0d-df56-4d46-b33a-aae9230a47fc,830be2fa-60a5-48cc-931f-34b670eae4bd,cfabdefe-0890-436e-b2a3-b5c712e22955,", PlatformMask = -1, FriendlyName = "Zipper Dress (Purple)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "2296ed0d-df56-4d46-b33a-aae9230a47fc,bbfa08e3-8e6b-4e0f-b264-1b398d7cd44a,cfabdefe-0890-436e-b2a3-b5c712e22955,", PlatformMask = -1, FriendlyName = "Zipper Dress (White)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "ecc1dbe6-ca06-4564-b2a6-30956194d1e9,484b6c13-af22-4ad5-8c43-34c0de095d49,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Wristbands (Light Blue)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "ecc1dbe6-ca06-4564-b2a6-30956194d1e9,f8b0cfe8-e129-4578-8bb5-f60af5d38599,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Wristbands (Green)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "ecc1dbe6-ca06-4564-b2a6-30956194d1e9,67bcca75-4ab1-4964-8688-9908c464d355,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Wristbands (Gold)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "7b857a8c-92ad-4028-a2c2-b3c20cdab5f2,1b1d08f2-12ca-43dd-a44f-ea2820b919b4,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Tank Top (Black)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "7b857a8c-92ad-4028-a2c2-b3c20cdab5f2,dee70c38-7a99-4c2b-9181-665f1bf75aca,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Tank Top (Blue)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "7b857a8c-92ad-4028-a2c2-b3c20cdab5f2,51ef8d39-2b94-4f9e-9620-07b6b0a913a5,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Tank Top (Orange)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "7b857a8c-92ad-4028-a2c2-b3c20cdab5f2,8377ab96-c908-457f-9fee-b784c9a759f3,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Tank Top (Red)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "6b9e022c-0b68-48fd-8eca-da8573c18900,5c4a2b35-0e1c-44de-8c3a-96d4a6458b1b,cf119781-5bd9-4b85-9a0b-12e82e988c23,", PlatformMask = -1, FriendlyName = "Long Scarf (Purple)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "6b9e022c-0b68-48fd-8eca-da8573c18900,6dd95046-acf8-42fe-ab78-80a334096a9d,cf119781-5bd9-4b85-9a0b-12e82e988c23,", PlatformMask = -1, FriendlyName = "Long Scarf (White)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "40528de7-38a3-4a7c-8f93-6d3bfa5573f2,dee70c38-7a99-4c2b-9181-665f1bf75aca,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Headband (Blue)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "40528de7-38a3-4a7c-8f93-6d3bfa5573f2,f8b0cfe8-e129-4578-8bb5-f60af5d38599,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Headband (Green)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "40528de7-38a3-4a7c-8f93-6d3bfa5573f2,8377ab96-c908-457f-9fee-b784c9a759f3,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Headband (Red)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "40528de7-38a3-4a7c-8f93-6d3bfa5573f2,,,", PlatformMask = -1, FriendlyName = "Headband (White)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "40528de7-38a3-4a7c-8f93-6d3bfa5573f2,67bcca75-4ab1-4964-8688-9908c464d355,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Headband (Yellow)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "24a240f4-1574-420b-b898-a7e91f170759,,,", PlatformMask = -1, FriendlyName = "Back Bun Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "c45ed7b8-99bd-4a4b-a9ff-e16edf5d7a18,,,", PlatformMask = -1, FriendlyName = "High Pony Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "14ef6b00-debf-4a85-9755-b4d37df496d3,484b6c13-af22-4ad5-8c43-34c0de095d49,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Baseball Cap (Light Blue)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "14ef6b00-debf-4a85-9755-b4d37df496d3,1b1d08f2-12ca-43dd-a44f-ea2820b919b4,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Baseball Cap (Black)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "14ef6b00-debf-4a85-9755-b4d37df496d3,51ef8d39-2b94-4f9e-9620-07b6b0a913a5,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Baseball Cap (Orange)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "14ef6b00-debf-4a85-9755-b4d37df496d3,,,", PlatformMask = -1, FriendlyName = "Baseball Cap (White)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "14ef6b00-debf-4a85-9755-b4d37df496d3,67bcca75-4ab1-4964-8688-9908c464d355,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Baseball Cap (Yellow)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "896c2491-2f96-4986-9cbd-b3b31ef5d8c5,,,", PlatformMask = -1, FriendlyName = "Equestrian Coat (Black)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "896c2491-2f96-4986-9cbd-b3b31ef5d8c5,55901f12-d5b5-4fa8-b4c8-e479689ee39d,d344b8cc-85a8-4ace-9f92-38c84f396e99,", PlatformMask = -1, FriendlyName = "Equestrian Coat (Blue)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "896c2491-2f96-4986-9cbd-b3b31ef5d8c5,4828b50c-95b6-466a-bb25-514891d78202,d344b8cc-85a8-4ace-9f92-38c84f396e99,", PlatformMask = -1, FriendlyName = "Equestrian Coat (Grey)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "896c2491-2f96-4986-9cbd-b3b31ef5d8c5,d6823e01-69f0-4f85-b94a-74894356a2cf,d344b8cc-85a8-4ace-9f92-38c84f396e99,", PlatformMask = -1, FriendlyName = "Equestrian Coat (Maroon)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "09177621-9ecd-4f6a-b6a5-64490139141d,,,", PlatformMask = -1, FriendlyName = "Flat Top Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "95ab7a7c-c35d-4da5-9955-0921064470b6,,,", PlatformMask = -1, FriendlyName = "Gekko Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "40528de7-38a3-4a7c-8f93-6d3bfa5573f2,0ecb8a2a-cffc-47db-aeda-fb0684aef1e5,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Headband (Grey)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "40528de7-38a3-4a7c-8f93-6d3bfa5573f2,484b6c13-af22-4ad5-8c43-34c0de095d49,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Headband (Light Blue)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "40528de7-38a3-4a7c-8f93-6d3bfa5573f2,51ef8d39-2b94-4f9e-9620-07b6b0a913a5,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Headband (Orange)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "40528de7-38a3-4a7c-8f93-6d3bfa5573f2,cbe29e9f-f2ac-47fb-97e1-8bad16abb89d,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Headband (Pink)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "40528de7-38a3-4a7c-8f93-6d3bfa5573f2,8377ab96-c908-457f-9fee-b784c9a759f3,018a5c07-e956-457d-a540-a5e2cd68da09,", PlatformMask = -1, FriendlyName = "Headband (Red, White)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "62ce4109-8dee-4895-bf1b-bfa143db4c7e,,,", PlatformMask = -1, FriendlyName = "Slim Blazer (Teal)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "62ce4109-8dee-4895-bf1b-bfa143db4c7e,cd5d7285-202d-42d0-b93f-04245875793e,0f36bb97-c61b-4281-929f-ff1d0d11be86,", PlatformMask = -1, FriendlyName = "Slim Blazer (Green)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "62ce4109-8dee-4895-bf1b-bfa143db4c7e,ad61c418-6d77-4a99-8ac5-9f10f5a3d42f,0f36bb97-c61b-4281-929f-ff1d0d11be86,", PlatformMask = -1, FriendlyName = "Slim Blazer (Blue)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "8b9f1413-e786-4a30-946c-9292f207875a,,,", PlatformMask = -1, FriendlyName = "Pulp Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "5cd08cfb-c729-4c30-96d9-6a99bb934d91,,,", PlatformMask = -1, FriendlyName = "Rec Room Sash", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "1a71064b-794f-40fa-9109-8ad36602b6e1,,,", PlatformMask = -1, FriendlyName = "Shagg Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "84cd594c-1cd8-4b4d-8409-85c8fd5fb02a,761a3193-60f0-4190-80c7-285b8192e794,91a451c1-b285-4c48-b14d-59ded8cc006f,", PlatformMask = -1, FriendlyName = "Stoll Dress (Blue)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "84cd594c-1cd8-4b4d-8409-85c8fd5fb02a,a819f49b-6c7a-49d3-9e6a-d9d79ef5019f,91a451c1-b285-4c48-b14d-59ded8cc006f,", PlatformMask = -1, FriendlyName = "Stoll Dress (Green)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "84cd594c-1cd8-4b4d-8409-85c8fd5fb02a,64850553-cdfe-455a-ac00-dafbe63d613e,91a451c1-b285-4c48-b14d-59ded8cc006f,", PlatformMask = -1, FriendlyName = "Stoll Dress (Orange)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "84cd594c-1cd8-4b4d-8409-85c8fd5fb02a,,,", PlatformMask = -1, FriendlyName = "Stoll Dress (Pink)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "71921831-ba6f-408b-a00e-2fd97663636f,1b1d08f2-12ca-43dd-a44f-ea2820b919b4,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Wrist Tape (Black)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "71921831-ba6f-408b-a00e-2fd97663636f,7d8e55fe-3c34-4b4b-9753-0021f6cc6454,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Wrist Tape (Cream)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "ecc1dbe6-ca06-4564-b2a6-30956194d1e9,0ecb8a2a-cffc-47db-aeda-fb0684aef1e5,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Wristbands (Grey)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "ecc1dbe6-ca06-4564-b2a6-30956194d1e9,7d8e55fe-3c34-4b4b-9753-0021f6cc6454,0b2395e1-ebcc-47e9-aaf1-faf9e9cec4cd,", PlatformMask = -1, FriendlyName = "Wristbands (Cream)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "6b9e022c-0b68-48fd-8eca-da8573c18900,,,", PlatformMask = -1, FriendlyName = "Long Scarf (Red)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "2e59d8d0-91a0-4449-bfdc-a5d663fd9343,0iSsaY-HgkmLaRHCn5vEdw,PioQ0o3yP0a6szPZ4EKs2A,", PlatformMask = -1, FriendlyName = "Collared Shirt (Blue)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "2e59d8d0-91a0-4449-bfdc-a5d663fd9343,jGj28vhq8EGwP2RuM074aQ,PioQ0o3yP0a6szPZ4EKs2A,", PlatformMask = -1, FriendlyName = "Collared Shirt (Yellow)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "2e59d8d0-91a0-4449-bfdc-a5d663fd9343,kmj5zOjcwku_WWKroCeiVQ,PioQ0o3yP0a6szPZ4EKs2A,", PlatformMask = -1, FriendlyName = "Collared Shirt (Pink)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "2e59d8d0-91a0-4449-bfdc-a5d663fd9343,FAviMCQ_EE2Mpt6QPo5OEw,PioQ0o3yP0a6szPZ4EKs2A,", PlatformMask = -1, FriendlyName = "Collared Shirt (Red)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "2e59d8d0-91a0-4449-bfdc-a5d663fd9343,MFrcSQ1DYUm8imvy4ypgvw,PioQ0o3yP0a6szPZ4EKs2A,", PlatformMask = -1, FriendlyName = "Collared Shirt (White)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "de0ac50d-2adb-4114-bd2e-68953b13d706,05ac07e1-67f0-486c-abf5-a62866475abb,be2b9293-1d3c-4b1c-b4c5-fad3ab16cf54,", PlatformMask = -1, FriendlyName = "Blazer (Black, Cream)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "0088603e-ec3b-4478-8694-e6fb1989b3f2,,,", PlatformMask = -1, FriendlyName = "Angled Bob Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "ffea7a65-613f-4835-921e-6dd15f357b7e,,,", PlatformMask = -1, FriendlyName = "Long Bangs Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "45f5e714-8a5f-4385-a97f-675066167011,,,", PlatformMask = -1, FriendlyName = "Seventies Stache", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "9bf5d259-7774-4cbe-a90f-7f188cc0dce7,,,", PlatformMask = -1, FriendlyName = "Thick Goatee", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "a6cbfe76-534a-4655-a8a8-3fed13d001c7,,,", PlatformMask = -1, FriendlyName = "Bald Top Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "CTcrvbo3OEepIV4oW8bx4w,,,", PlatformMask = -1, FriendlyName = "Receding Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "-twtjyBdQ02EAdOfBGTiEw,,,", PlatformMask = -1, FriendlyName = "Van Dyke Beard", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "45eaab67-19c2-4601-8f80-3565a4dceba4,,,", PlatformMask = -1, FriendlyName = "Pompadour Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "c855dcc3-96cb-470d-b159-d37a025a47d1,,,", PlatformMask = -1, FriendlyName = "Dutch Braid Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "d7730a9e-78a1-4356-bc09-6b066615850b,,,", PlatformMask = -1, FriendlyName = "Afro Updo Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "8c35c804-e8d5-49d2-8d5a-ea19fb70bfa6,,,", PlatformMask = -1, FriendlyName = "Pencil Bun Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "5beeb4c4-f276-4eae-87aa-9302e45b05b7,,,", PlatformMask = -1, FriendlyName = "Cornrows Hair", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "b6rLwzD4NkKV7xKn9ZYVkA,sxUE0iOSZEmezm54T7xI3Q,tlpa7195x0CkmSjpR1RArQ,", PlatformMask = -1, FriendlyName = "Rec Room Hoodie - Pride (Rainbow Pride)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "fe15ca53-c5b8-4acf-9309-ff3f4e610fc9,,,", PlatformMask = -1, FriendlyName = "Winged Hat - Pride (Rainbow Pride)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "b6rLwzD4NkKV7xKn9ZYVkA,D_Xmo0rOzkS-kgq1CYXt3g,tnCJp2eDI0SwjVfJMhk3LQ,", PlatformMask = -1, FriendlyName = "Rec Room Hoodie - Pride (Trans Pride)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "fe15ca53-c5b8-4acf-9309-ff3f4e610fc9,knXPidb-Rkayfc3kSHfZeQ,1yMyo6oTjU-VAygoeWaohQ,", PlatformMask = -1, FriendlyName = "Winged Hat - Pride (Trans Pride)", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "88b6ddeb-a455-460d-91d9-a4569ef6903c,,,", PlatformMask = -1, FriendlyName = "Square Earrings ", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "0abb6b08-20ce-444f-879e-0d1344df096c,,,", PlatformMask = -1, FriendlyName = "Round Earrings", Tooltip = "", Rarity = 0 },
                new { AvatarItemType = 0, AvatarItemDesc = "9b5bde11-7408-4798-9fcb-c7ec175444df,,,", PlatformMask = -1, FriendlyName = "Hoop Earrings", Tooltip = "", Rarity = 0 }
            };

            var allItems = ownedItems.Cast<object>().Concat(defaultItems).ToList();
            
            return Results.Json(allItems);
        });

        app.MapGet("/api/avatar/v2", async (HttpRequest request, AppDbContext db) =>
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
            
            var avatar = await db.PlayerAvatars
                .FirstOrDefaultAsync(a => a.OwnerAccountId == id);
            
            if (avatar == null)
            {
                avatar = new PlayerAvatar
                {
                    OwnerAccountId = id,
                    OutfitSelections = "",
                    FaceFeatures = "{}",
                    SkinColor = "",
                    HairColor = ""
                };
                db.PlayerAvatars.Add(avatar);
                await db.SaveChangesAsync();
            }
            
            return Results.Json(new
            {
                OutfitSelections = avatar.OutfitSelections,
                FaceFeatures = avatar.FaceFeatures,
                SkinColor = avatar.SkinColor,
                HairColor = avatar.HairColor
            });
        });

        app.MapPost("/api/avatar/v2/set", async (HttpRequest request, AppDbContext db) =>
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
        });
        
        app.MapGet("/api/PlayerReporting/v1/moderationBlockDetails", () => 
            Results.Content("{\"ReportCategory\":0,\"Duration\":0,\"GameSessionId\":0,\"IsHostKick\":false,\"Message\":\"\",\"PlayerIdReporter\":null,\"IsBan\":false}", "application/json"));
        
        app.MapGet("/api/PlayerReporting/v1/voteToKickReasons", async (HttpRequest request, AppDbContext db) =>
        {
            var json = File.ReadAllText("JSON/vtkreasons.json");
            return Results.Content(json, "application/json");
        });

        app.MapGet("/api/settings/v2" +
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
        
        app.MapPost("/api/settings/v2/set", async (HttpRequest request, AppDbContext db) =>
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

            request.EnableBuffering();
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();
            
            Console.WriteLine($"Settings request body: {body}");
            
            var settings = new List<PlayerSetting>();
            
            if (body.TrimStart().StartsWith("["))
            {
                settings = System.Text.Json.JsonSerializer.Deserialize<List<PlayerSetting>>(body) ?? [];
            }
            else
            {
                var single = System.Text.Json.JsonSerializer.Deserialize<PlayerSetting>(body);
                if (single != null) settings.Add(single);
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
        
        app.MapGet("/api/objectives/v1/myprogress", async (HttpRequest request, AppDbContext db) =>
        {
            // TODO ADD FUNCTIONALITY
            var json = File.ReadAllText("JSON/tempmyprogress.json");
            return Results.Content(json, "application/json");
        });
        app.MapGet("/api/communityboard/v2/current", async (HttpRequest request, AppDbContext db) =>
        {
            var json = File.ReadAllText("JSON/communityboard.json");
            return Results.Content(json, "application/json");
        });
        app.MapGet("/api/playerevents/v1/all", async (HttpRequest request, AppDbContext db) =>
        {
            // TODO ADD FUNCTIONALITY
            return Results.Content("{\"Created\":[],\"Responses\":[]}", "application/json");
        });
        app.MapGet("/api/storefronts/v1/p2p/betaEnabled", async (HttpRequest request, AppDbContext db) =>
        {
            return "false";
        });
        app.MapGet("/api/announcement/v1/get", async (HttpRequest request, AppDbContext db) =>
        {
            var json = File.ReadAllText("JSON/announcements.json");
            return Results.Content(json, "application/json");
        });
        app.MapGet("/api/quickPlay/v1/getandclear", async (HttpRequest request, AppDbContext db) =>
        {
            // TODO ADD FUNCTIONALITY
            return Results.Content("{\"RoomName\":null,\"ActionCode\":null,\"TargetPlayerId\":null}", "application/json");
        });
        app.MapGet("/api/roomkeys/v1/room", async (HttpRequest request, AppDbContext db) =>
        {
            // TODO ADD FUNCTIONALITY
            var roomid = request.Query["roomId"];
            return "[]";
        });
        app.MapGet("/api/accounts/v1/getBio", async (HttpRequest request, AppDbContext db) =>
        {
            var authHeader = request.Headers.Authorization.ToString();
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Results.Unauthorized();

            var token = authHeader.Substring("Bearer ".Length);
            var accountId = jwtService.ValidateAndGetAccountId(token);

            if (string.IsNullOrEmpty(accountId) || !int.TryParse(accountId.AsSpan(), out var id))
                return Results.Unauthorized();

            var bio = await db.PlayerBios
                .Where(s => accountId == accountId)
                .ToListAsync();
            
            return Results.Json(bio);
        });
        app.MapGet("/roomserver/rooms/bulk", async (HttpRequest request, AppDbContext db) =>
        {
            var idParam = request.Query["id"].FirstOrDefault();
            var nameParam = request.Query["name"].FirstOrDefault();

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
            {
                return Results.Json(new object[0]);
            }

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
        });

        app.MapGet("/roomserver/rooms/hot", async (HttpRequest request, AppDbContext db) =>
        {
            var tagFilter = request.Query["tag"].FirstOrDefault()?.ToLower();
            
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

            var hotRooms = allRooms
                .OrderByDescending(r => r.Id)
                .ToList();

            var results = new List<object>();

            foreach (var room in hotRooms)
            {
                var subRooms = await db.SubRooms.Where(x => x.RoomId == room.RoomId).ToListAsync();
                var roles = await db.RoomRoles.Where(x => x.RoomId == room.RoomId).ToListAsync();
                var loadScreens = await db.LoadScreens.Where(x => x.RoomId == room.RoomId).ToListAsync();
                var promoImages = await db.PromoImages.Where(x => x.RoomId == room.RoomId).ToListAsync();
                var external = await db.PromoExternalContents.Where(x => x.RoomId == room.RoomId).ToListAsync();
                var tags = TryDeserializeTags(room.Tags) ?? new object[0];

                var roomResponse = new
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
                    SubRooms = subRooms.Select(s => new
                    {
                        s.SubRoomId,
                        s.RoomId,
                        s.Name,
                        s.DataBlob,
                        s.IsSandbox,
                        s.MaxPlayers,
                        s.Accessibility,
                        s.UnitySceneId,
                        s.DataSavedAt
                    }),
                    Roles = roles.Select(r => new
                    {
                        r.AccountId,
                        r.Role,
                        r.InvitedRole
                    }),
                    LoadScreens = loadScreens.Select(ls => new
                    {
                        ls.ImageUrl,
                        ls.Tooltip,
                        ls.IsThumbnail
                    }),
                    PromoImages = promoImages.Select(pi => new
                    {
                        pi.ImageUrl,
                        pi.Tooltip,
                        pi.SortOrder
                    }),
                    PromoExternalContent = external.Select(ec => new
                    {
                        ec.Type,
                        ec.Url,
                        ec.Tooltip
                    }),
                    Tags = tags
                };
                results.Add(roomResponse);
            }

            var response = new
            {
                Results = results,
                TotalResults = results.Count
            };

            return Results.Json(response);
        });

        app.MapGet("/roomserver/roomsandplaylists/hot", async (HttpRequest request, AppDbContext db) =>
        {
            // should sort by popularity but idk how to yet so we just d it by id order
            var allRooms = await db.Rooms
                .Where(r => !r.IsDorm)
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            var results = new List<object>();

            foreach (var room in allRooms)
            {
                var subRooms = await db.SubRooms.Where(x => x.RoomId == room.RoomId).ToListAsync();
                var roles = await db.RoomRoles.Where(x => x.RoomId == room.RoomId).ToListAsync();
                var loadScreens = await db.LoadScreens.Where(x => x.RoomId == room.RoomId).ToListAsync();
                var promoImages = await db.PromoImages.Where(x => x.RoomId == room.RoomId).ToListAsync();
                var external = await db.PromoExternalContents.Where(x => x.RoomId == room.RoomId).ToListAsync();
                var tags = TryDeserializeTags(room.Tags) ?? new object[0];

                var roomResponse = new
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
                    SubRooms = subRooms.Select(s => new
                    {
                        s.SubRoomId,
                        s.RoomId,
                        s.Name,
                        s.DataBlob,
                        s.IsSandbox,
                        s.MaxPlayers,
                        s.Accessibility,
                        s.UnitySceneId,
                        s.DataSavedAt
                    }),
                    Roles = roles.Select(r => new
                    {
                        r.AccountId,
                        r.Role,
                        r.InvitedRole
                    }),
                    LoadScreens = loadScreens.Select(ls => new
                    {
                        ls.ImageUrl,
                        ls.Tooltip,
                        ls.IsThumbnail
                    }),
                    PromoImages = promoImages.Select(pi => new
                    {
                        pi.ImageUrl,
                        pi.Tooltip,
                        pi.SortOrder
                    }),
                    PromoExternalContent = external.Select(ec => new
                    {
                        ec.Type,
                        ec.Url,
                        ec.Tooltip
                    }),
                    Tags = tags
                };
                results.Add(roomResponse);
            }

            var response = new
            {
                Results = results,
                TotalResults = results.Count
            };

            return Results.Json(response);
        });
        app.MapGet("/roomserver/rooms/{id}", async (HttpRequest request, AppDbContext db, string id) =>
        {
            // this sucked to do
            if (!int.TryParse(id, out var roomId))
                return Results.NotFound();

            var room = await db.Rooms
                .FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null)
                return Results.NotFound();

            var subRooms = true
                ? await db.SubRooms.Where(x => x.RoomId == room.RoomId).ToListAsync()
                : new List<SubRoom>();

            var roles = true
                ? await db.RoomRoles.Where(x => x.RoomId == room.RoomId).ToListAsync()
                : new List<RoomRole>();

            var loadScreens = true
                ? await db.LoadScreens.Where(x => x.RoomId == room.RoomId).ToListAsync()
                : new List<LoadScreen>();

            var promoImages = true
                ? await db.PromoImages.Where(x => x.RoomId == room.RoomId).ToListAsync()
                : new List<PromoImage>();

            var external = true
                ? await db.PromoExternalContents.Where(x => x.RoomId == room.RoomId).ToListAsync()
                : new List<PromoExternalContent>();

            var tags = TryDeserializeTags(room.Tags) ?? new object[0];

            var response = new
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

                Stats = new RoomStats
                {
                    CheerCount = 0,
                    FavoriteCount = 0,
                    VisitorCount = 1,
                    VisitCount = 1
                },

                SubRooms = subRooms.Select(s => new
                {
                    s.SubRoomId,
                    s.RoomId,
                    s.Name,
                    s.DataBlob,
                    s.IsSandbox,
                    s.MaxPlayers,
                    s.Accessibility,
                    s.UnitySceneId,
                    s.DataSavedAt
                }).ToList(),

                Roles = roles.Select(r => new
                {
                    r.AccountId,
                    r.Role,
                    r.InvitedRole
                }).ToList(),

                LoadScreens = loadScreens.Select(l => new
                {
                    l.ImageUrl,
                    l.Tooltip,
                    l.IsThumbnail
                }).ToList(),

                PromoImages = promoImages.Select(p => new
                {
                    p.ImageUrl,
                    p.Tooltip,
                    p.SortOrder
                }).ToList(),

                PromoExternalContent = external.Select(e => new
                {
                    e.Type,
                    e.Url,
                    e.Tooltip
                }).ToList(),

                Tags = tags
            };

            return Results.Json(response);
        });
        app.MapGet("/api/images/v2/named", async (HttpRequest request, AppDbContext db) =>
        {
            var json = File.ReadAllText("JSON/namedimages.json");
            return Results.Content(json, "application/json");
        });
        app.MapPost("/api/objectives/v1/updateobjective", async (HttpRequest request, AppDbContext db) =>
        {
            // TODO: implement saving daily
            return Results.Ok();
        });
        app.MapPost("/api/PlayerReporting/v1/hile", async (HttpRequest request, AppDbContext db) =>
        {
            // stops crashing the game due to bepinex winhttp.dll ( or melonloader version.dll)
            return Results.Ok();
        });
        app.MapGet("/api/storefronts/v3/giftdropstore/300", async (HttpRequest request, AppDbContext db) =>
        {
            var storefronts = await storefrontService.GetStorefrontsAsync();
            var storefront = storefronts.FirstOrDefault(s => s.StorefrontType == 300 && s.Name == "rc_cafe_storefront");
            if (storefront == null)
            {
                var json = File.ReadAllText("JSON/cafestorefront.json");
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
                    gd.Id,
                    gd.StorefrontItemId,
                    gd.GiftDropId,
                    gd.FriendlyName,
                    gd.Tooltip,
                    gd.ConsumableItemDesc,
                    gd.AvatarItemDesc,
                    gd.AvatarItemType,
                    gd.EquipmentPrefabName,
                    gd.EquipmentModificationGuid,
                    gd.IsQuery,
                    gd.Unique,
                    gd.SubscribersOnly,
                    gd.Level,
                    gd.Rarity,
                    gd.CurrencyType,
                    gd.Currency,
                    gd.Context,
                    gd.ItemSetId,
                    gd.ItemSetFriendlyName
                }).ToList(),
                Prices = item.Prices.Select(p => new
                {
                    p.Id,
                    p.StorefrontItemId,
                    p.CurrencyType,
                    p.Price
                }).ToList()
            }).ToList();
            var response = new { storefront.Id, storefront.Name, storefront.StorefrontType, storefront.NextUpdate, StoreItems = storeItems };
            return Results.Ok(response);
        });
        app.MapGet("/api/storefronts/v3/giftdropstore/2", async (HttpRequest request, AppDbContext db) =>
        {
            var storefronts = await storefrontService.GetStorefrontsAsync();
            var storefront = storefronts.FirstOrDefault(s => s.StorefrontType == 2 && s.Name == "rec_center_store");
            if (storefront == null)
            {
                var json = File.ReadAllText("JSON/storefront12.json");
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
                    gd.Id,
                    gd.StorefrontItemId,
                    gd.GiftDropId,
                    gd.FriendlyName,
                    gd.Tooltip,
                    gd.ConsumableItemDesc,
                    gd.AvatarItemDesc,
                    gd.AvatarItemType,
                    gd.EquipmentPrefabName,
                    gd.EquipmentModificationGuid,
                    gd.IsQuery,
                    gd.Unique,
                    gd.SubscribersOnly,
                    gd.Level,
                    gd.Rarity,
                    gd.CurrencyType,
                    gd.Currency,
                    gd.Context,
                    gd.ItemSetId,
                    gd.ItemSetFriendlyName
                }).ToList(),
                Prices = item.Prices.Select(p => new
                {
                    p.Id,
                    p.StorefrontItemId,
                    p.CurrencyType,
                    p.Price
                }).ToList()
            }).ToList();
            var response = new { storefront.Id, storefront.Name, storefront.StorefrontType, storefront.NextUpdate, StoreItems = storeItems };
            return Results.Ok(response);
        });
        app.MapPost("/api/storefronts/v2/buyItem", async (HttpRequest request, BuyItemRequest buyRequest, AppDbContext db, JwtTokenService jwtService) =>
        {
            try
            {
                var authHeader = request.Headers.Authorization.ToString();
            
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    return Results.Unauthorized();

                var token = authHeader.Substring("Bearer ".Length);
                var accountId = jwtService.ValidateAndGetAccountId(token);

                if (string.IsNullOrEmpty(accountId) || !int.TryParse(accountId.AsSpan(), out var id))
                    return Results.Unauthorized();

                var storefrontItem = await db.StorefrontItems
                    .Include(si => si.Prices)
                    .Include(si => si.GiftDrops)
                    .FirstOrDefaultAsync(si => si.PurchasableItemId == buyRequest.PurchasableItemId);

                if (storefrontItem == null)
                {
                    return Results.NotFound(new { error = "Item not found" });
                }

                var price = storefrontItem.Prices.FirstOrDefault(p => p.CurrencyType == buyRequest.CurrencyType);
                if (price == null)
                {
                    return Results.BadRequest(new { error = "Currency type not available for this item" });
                }

                var tokenBalance = await db.TokenBalances
                    .FirstOrDefaultAsync(tb => tb.Id == id && 
                                              tb.CurrencyType == buyRequest.CurrencyType && 
                                              tb.BalanceType == -1);

                if (tokenBalance == null)
                {
                    return Results.BadRequest(new { error = "Account has no token balance" });
                }

                if (tokenBalance.Balance < price.Price)
                {
                    return Results.BadRequest(new { error = "Insufficient balance" });
                }

                tokenBalance.Balance -= price.Price;
                db.TokenBalances.Update(tokenBalance);
                await db.SaveChangesAsync();

                var receiverAccountId = id;
                int? senderPlayerId = null;
                
                if (buyRequest.Gift != null)
                {
                    receiverAccountId = buyRequest.Gift.ToPlayerId;
                    if (!buyRequest.Gift.Anonymous)
                    {
                        senderPlayerId = id;
                    }
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
                       // FriendlyName = giftDrop.FriendlyName ?? string.Empty,
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
                    Id = rg.Id,
                    FromPlayerId = null,
                    ConsumableItemDesc = rg.ConsumableItemDesc,
                    AvatarItemDesc = rg.AvatarItemDesc,
                    FriendlyName = rg.FriendlyName,
                    AvatarItemType = rg.AvatarItemType,
                    EquipmentPrefabName = rg.EquipmentPrefabName,
                    EquipmentModificationGuid = rg.EquipmentModificationGuid,
                    CurrencyType = rg.CurrencyType,
                    Currency = rg.Currency,
                    Xp = rg.Xp,
                    Level = rg.Level,
                    Platform = rg.Platform,
                    PlatformsToSpawnOn = rg.PlatformsToSpawnOn,
                    BalanceType = rg.BalanceType,
                    GiftContext = rg.GiftContext,
                    GiftRarity = rg.GiftRarity,
                    Message = rg.Message
                }).ToList();

                var balanceUpdate = new BalanceUpdate
                {
                    UpdateResponse = 0,
                    Data = giftData
                };

                var response = new BuyItemResponse
                {
                    BalanceUpdates = new List<BalanceUpdate> { balanceUpdate },
                    Balance = tokenBalance.Balance,
                    CurrencyType = buyRequest.CurrencyType,
                    BalanceType = -1,
                    Platform = -1
                };

                try
                {
                    var purchaseNotification = notificationService.CreateNotification(
                        PushNotificationId.StorefrontBalancePurchase,
                        id: storefrontItem.Id,
                        toAccountId: id,
                        data: new Dictionary<string, object>
                        {
                            { "BalanceAddType", 1400 },
                            { "Delta", -price.Price },
                            { "Balance", tokenBalance.Balance },
                            { "CurrencyType", buyRequest.CurrencyType }
                        }
                    );
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
        });

        app.MapPost("/api/avatar/v2/gifts/generate", async (HttpRequest request, AppDbContext db, JwtTokenService jwtService) =>
        {
            try
            {
                var authHeader = request.Headers.Authorization.ToString();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    return Results.Unauthorized();

                var token = authHeader.Substring("Bearer ".Length);
                var accountId = jwtService.ValidateAndGetAccountId(token);

                if (string.IsNullOrEmpty(accountId) || !int.TryParse(accountId.AsSpan(), out var id))
                    return Results.Unauthorized();

                request.EnableBuffering();
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body);
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

                        var unownedRewards = avatarRewards
                            .Where(ar => !ownedAvatarItems.Contains(ar.AvatarItemDesc))
                            .ToList();

                        if (unownedRewards.Any())
                        {
                            var selectedReward = unownedRewards[random.Next(unownedRewards.Count)];
                            avatarItemDesc = selectedReward.AvatarItemDesc;
                            friendlyName = selectedReward.FriendlyName;
                            avatarItemType = selectedReward.AvatarItemType;
                            giftRarity = selectedReward.GiftRarity;
                        }
                        else
                        {
                            isEarnableItem = false;
                        }
                    }
                    else
                    {
                        isEarnableItem = false;
                    }
                }

                if (!isEarnableItem)
                {
                    int[] tokenAmounts = {10, 25, 50, 100, 250, 500};
                    currency = tokenAmounts[random.Next(tokenAmounts.Length)];
                    currencyType = 2;
                    giftRarity = 20;
                }
                
                var receivedGift = new ReceivedGift
                {
                    ReceiverAccountId = id,
                    FromPlayerId = 1,
                    Message = message,
                    ConsumableItemDesc = consumableItemDesc,
                    AvatarItemDesc = avatarItemDesc,
                    FriendlyName = friendlyName,
                    AvatarItemType = avatarItemType,
                    EquipmentPrefabName = "",
                    EquipmentModificationGuid = "",
                    CurrencyType = currencyType,
                    Currency = currency,
                    Xp = xp,
                    Level = 0,
                    Platform = -1,
                    PlatformsToSpawnOn = -1,
                    BalanceType = 0,
                    GiftContext = giftContext,
                    GiftRarity = giftRarity,
                    ReceivedAt = DateTime.UtcNow,
                    IsConsumed = false
                };
                
                db.ReceivedGifts.Add(receivedGift);
                await db.SaveChangesAsync();

                var giftData = new
                {
                    Id = receivedGift.Id,
                    FromPlayerId = 1,
                    ConsumableItemDesc = consumableItemDesc,
                    AvatarItemDesc = avatarItemDesc,
                    FriendlyName = friendlyName,
                    AvatarItemType = avatarItemType,
                    EquipmentPrefabName = "",
                    EquipmentModificationGuid = "",
                    CurrencyType = currencyType,
                    Currency = currency,
                    Xp = xp,
                    Level = 0,
                    Platform = -1,
                    PlatformsToSpawnOn = -1,
                    BalanceType = 0,
                    GiftContext = giftContext,
                    GiftRarity = giftRarity,
                    Message = message
                };

                return Results.Ok(giftData);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error generating gift: {ex.Message}");
            }
        });

        app.MapPost("/api/avatar/v2/gifts/consume/", async (HttpRequest request, AppDbContext db, JwtTokenService jwtService) =>
        {
            try
            {
                var authHeader = request.Headers.Authorization.ToString();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    return Results.Unauthorized();

                var token = authHeader.Substring("Bearer ".Length);
                var accountId = jwtService.ValidateAndGetAccountId(token);

                if (string.IsNullOrEmpty(accountId) || !int.TryParse(accountId.AsSpan(), out var id))
                    return Results.Unauthorized();

                request.EnableBuffering();
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body);
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
                {
                    return Results.BadRequest(new { success = false, error = "Invalid gift ID" });
                }

                var receivedGift = await db.ReceivedGifts
                    .FirstOrDefaultAsync(rg => rg.Id == giftId && rg.ReceiverAccountId == id);

                if (receivedGift == null)
                {
                    return Results.NotFound(new { success = false, error = "Gift not found" });
                }

                if (!string.IsNullOrEmpty(receivedGift.ConsumableItemDesc))
                {
                    var existingConsumable = await db.ConsumableItems
                        .FirstOrDefaultAsync(c => c.OwnerAccountId == id && c.ConsumableItemDesc == receivedGift.ConsumableItemDesc);

                    if (existingConsumable == null)
                    {
                        var newConsumable = new ConsumableItem
                        {
                            OwnerAccountId = id,
                            Ids = new List<int> { giftId },
                            CreatedAts = new List<DateTime> { DateTime.UtcNow },
                            ConsumableItemDesc = receivedGift.ConsumableItemDesc,
                            Count = 1,
                            InitialCount = 1,
                            IsActive = false,
                            ActiveDurationMinutes = 0,
                            IsTransferable = false
                        };
                        db.ConsumableItems.Add(newConsumable);
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
                    var avatarItem = new AvatarItem
                    {
                        OwnerAccountId = id,
                        AvatarItemDesc = receivedGift.AvatarItemDesc,
                        FriendlyName = receivedGift.FriendlyName ?? ""
                    };
                    db.AvatarItems.Add(avatarItem);
                }
                
                if (!string.IsNullOrEmpty(receivedGift.Currency.ToString()) && receivedGift.CurrencyType == 2)
                {
                    var tokenBalance = await db.TokenBalances
                        .FirstOrDefaultAsync(tb => tb.Id == id && 
                                                  tb.CurrencyType == receivedGift.CurrencyType && 
                                                  tb.BalanceType == -1);

                    if (tokenBalance == null)
                    {
                        tokenBalance = new TokenBalance
                        {
                            Id = id,
                            CurrencyType = receivedGift.CurrencyType,
                            BalanceType = -1,
                            Balance = receivedGift.Currency
                        };
                        db.TokenBalances.Add(tokenBalance);
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
        });
        app.MapGet("/api/rooms/v1/filters", async (HttpRequest request, AppDbContext db) =>
        {
            var json = File.ReadAllText("JSON/roomfilters.json");
            return Results.Content(json, "application/json");
        });
        app.MapGet("/roomserver/rooms/{id}/interactionby/me", async (HttpRequest request, AppDbContext db, string id) =>
        {
            // TODO: implement
            return Results.Content("{\"Cheered\":false,\"Favorited\":false}", "application/json");
        });
        app.MapPost("/api/images/v4/uploadsaved", async (HttpContext context) =>
        {
            try
            {
                var form = await context.Request.ReadFormAsync();
                var file = form.Files.FirstOrDefault();

                if (file == null)
                {
                    return Results.BadRequest(new { error = "No file found in request" });
                }

                var imageId = Guid.NewGuid().ToString("N");
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                var validExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp" };
                if (string.IsNullOrEmpty(extension) || !validExtensions.Contains(extension))
                {
                    extension = ".png";
                }

                var savedFileName = imageId + extension;
                var filePath = Path.Combine(ImagesDir, savedFileName);

                using (var fileStream = File.Create(filePath))
                {
                    await file.CopyToAsync(fileStream);
                }

                return Results.Ok(new
                {
                    ImageName = savedFileName
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error uploading image: {ex.Message}");
            }
        });
        
         app.MapGet("/api/config/v1/backtrace", () => Results.Ok(new
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
         }));
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
