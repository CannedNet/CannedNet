using System.Text.Json;
using CannedNet.Data;
using CannedNet.Models;
using Microsoft.EntityFrameworkCore;

namespace CannedNet.Services;

public class StorefrontFillService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<StorefrontFillService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly ConfigService _config;

    public StorefrontFillService(AppDbContext dbContext, ILogger<StorefrontFillService> logger, IWebHostEnvironment environment, ConfigService config)
    {
        _dbContext = dbContext;
        _logger = logger;
        _environment = environment;
        _config = config;
    }

    public async Task FillStorefrontsAsync()
    {
        try
        {
            if (await _dbContext.Storefronts.AnyAsync())
            {
                return;
            }

            if (!_config.Config.AutoFillStorefronts)
            {
                return;
            }

            _logger.LogInformation("automatically filling out storefronts due to no data");

            var storefronts = new[]
            {
                new { Name = "watch_store", FilePath = "storefront3.json", Type = 2 },
                new { Name = "rec_center_store", FilePath = "storefront12.json", Type = 2 },
                new { Name = "rc_cafe_storefront", FilePath = "cafestorefront.json", Type = 300 }
            };

            foreach (var storeInfo in storefronts)
            {
                await FillStorefrontFromJsonAsync(storeInfo.Name, storeInfo.FilePath, storeInfo.Type);
            }

            _logger.LogInformation("storefronts complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "error during automatically filling out storefronts");
            throw;
        }
    }

    public async Task<List<Storefront>> GetStorefrontsAsync()
    {
        return await _dbContext.Storefronts
            .Include(s => s.Items)
            .ThenInclude(si => si.GiftDrops)
            .Include(s => s.Items)
            .ThenInclude(si => si.Prices)
            .ToListAsync();
    }

    private async Task FillStorefrontFromJsonAsync(string name, string jsonFileName, int storefrontType)
    {
        try
        {
            string jsonPath = Path.Combine(_environment.ContentRootPath, "JSON", jsonFileName);

            if (!File.Exists(jsonPath))
            {
                _logger.LogWarning($"file not found: {jsonPath}");
                return;
            }

            string jsonContent = await File.ReadAllTextAsync(jsonPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var storefront = new Storefront
            {
                Name = name,
                StorefrontType = storefrontType,
                NextUpdate = root.TryGetProperty("NextUpdate", out var nextUpdate)
                    ? DateTime.SpecifyKind(DateTime.Parse(nextUpdate.GetString() ?? DateTime.UtcNow.ToString()), DateTimeKind.Utc)
                    : DateTime.UtcNow.AddDays(7)
            };

            _dbContext.Storefronts.Add(storefront);
            await _dbContext.SaveChangesAsync();

            if (root.TryGetProperty("StoreItems", out var storeItemsArray))
            {
                foreach (var itemElement in storeItemsArray.EnumerateArray())
                {
                    var item = new StorefrontItem
                    {
                        StorefrontId = storefront.Id,
                        PurchasableItemId = itemElement.GetProperty("PurchasableItemId").GetInt32(),
                        Type = itemElement.GetProperty("Type").GetInt32(),
                        IsFeatured = itemElement.GetProperty("IsFeatured").GetBoolean(),
                        NewUntil = itemElement.TryGetProperty("NewUntil", out var newUntil) && newUntil.ValueKind != JsonValueKind.Null
                            ? DateTime.SpecifyKind(DateTime.Parse(newUntil.GetString() ?? DateTime.UtcNow.ToString()), DateTimeKind.Utc)
                            : null
                    };

                    _dbContext.StorefrontItems.Add(item);
                    await _dbContext.SaveChangesAsync();

                    // giftdrops
                    if (itemElement.TryGetProperty("GiftDrops", out var giftDropsArray))
                    {
                        foreach (var dropElement in giftDropsArray.EnumerateArray())
                        {
                            var drop = new GiftDrop
                            {
                                StorefrontItemId = item.Id,
                                GiftDropId = dropElement.GetProperty("GiftDropId").GetInt32(),
                                FriendlyName = dropElement.GetProperty("FriendlyName").GetString() ?? string.Empty,
                                Tooltip = dropElement.TryGetProperty("Tooltip", out var tooltip) ? tooltip.GetString() : null,
                                ConsumableItemDesc = dropElement.TryGetProperty("ConsumableItemDesc", out var consumableDesc) && consumableDesc.ValueKind != JsonValueKind.Null
                                    ? consumableDesc.GetString()
                                    : null,
                                AvatarItemDesc = dropElement.TryGetProperty("AvatarItemDesc", out var avatarDesc) && avatarDesc.ValueKind != JsonValueKind.Null
                                    ? avatarDesc.GetString()
                                    : null,
                                AvatarItemType = dropElement.GetProperty("AvatarItemType").GetInt32(),
                                EquipmentPrefabName = dropElement.TryGetProperty("EquipmentPrefabName", out var prefab) && prefab.ValueKind != JsonValueKind.Null
                                    ? prefab.GetString()
                                    : null,
                                EquipmentModificationGuid = dropElement.TryGetProperty("EquipmentModificationGuid", out var guid) && guid.ValueKind != JsonValueKind.Null
                                    ? guid.GetString()
                                    : null,
                                IsQuery = dropElement.GetProperty("IsQuery").GetBoolean(),
                                Unique = dropElement.GetProperty("Unique").GetBoolean(),
                                SubscribersOnly = dropElement.GetProperty("SubscribersOnly").GetBoolean(),
                                Level = dropElement.GetProperty("Level").GetInt32(),
                                Rarity = dropElement.GetProperty("Rarity").GetInt32(),
                                CurrencyType = dropElement.GetProperty("CurrencyType").GetInt32(),
                                Currency = dropElement.GetProperty("Currency").GetInt32(),
                                Context = dropElement.GetProperty("Context").GetInt32(),
                                ItemSetId = dropElement.TryGetProperty("ItemSetId", out var itemSetId) && itemSetId.ValueKind != JsonValueKind.Null
                                    ? itemSetId.GetInt32()
                                    : null,
                                ItemSetFriendlyName = dropElement.TryGetProperty("ItemSetFriendlyName", out var itemSetName) && itemSetName.ValueKind != JsonValueKind.Null
                                    ? itemSetName.GetString()
                                    : null
                            };

                            _dbContext.GiftDrops.Add(drop);
                        }
                    }

                    // prices
                    if (itemElement.TryGetProperty("Prices", out var pricesArray))
                    {
                        foreach (var priceElement in pricesArray.EnumerateArray())
                        {
                            var price = new StorefrontPrice
                            {
                                StorefrontItemId = item.Id,
                                CurrencyType = priceElement.GetProperty("CurrencyType").GetInt32(),
                                Price = priceElement.GetProperty("Price").GetInt32()
                            };

                            _dbContext.StorefrontPrices.Add(price);
                        }
                    }
                }
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"success on storefront: {name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"error filling out storefront: {jsonFileName}");
            throw;
        }
    }
}
