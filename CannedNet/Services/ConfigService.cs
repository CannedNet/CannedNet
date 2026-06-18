using System.Text.Json;

namespace CannedNet.Services;

public class ConfigService
{
    private const string ConfigPath = "config.json";
    private const string EndpointsPath = "JSON/endpoints.json";
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public AppConfig Config { get; private set; }

    public ConfigService()
    {
        LoadConfig();
        SyncBaseUrlToEndpoints();
    }

    private void LoadConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            Config = CreateDefaultConfig();
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(Config, Indented));
        }
        else
        {
            var json = File.ReadAllText(ConfigPath);
            Config = JsonSerializer.Deserialize<AppConfig>(json) ?? CreateDefaultConfig();
        }
    }

    public void SaveConfig(AppConfig updated)
    {
        Config = updated;
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(Config, Indented));
        SyncBaseUrlToEndpoints();
    }

    private void SyncBaseUrlToEndpoints()
    {
        var configBase = Config.BaseUrl?.TrimEnd('/');
        if (string.IsNullOrEmpty(configBase))
            return;

        if (!File.Exists(EndpointsPath))
            return;

        var endpointsJson = File.ReadAllText(EndpointsPath);
        var endpoints = JsonSerializer.Deserialize<Dictionary<string, string>>(endpointsJson);
        if (endpoints == null || endpoints.Count == 0)
            return;

        var currentBase = DetectBaseUrl(endpoints);
        if (currentBase == null)
            return;

        if (string.Equals(currentBase, configBase, StringComparison.OrdinalIgnoreCase))
            return;

        var updated = new Dictionary<string, string>(endpoints.Count);
        foreach (var (key, url) in endpoints)
        {
            if (url.StartsWith(currentBase, StringComparison.OrdinalIgnoreCase))
            {
                var path = url[currentBase.Length..];
                updated[key] = configBase + path;
            }
            else
            {
                updated[key] = url;
            }
        }

        File.WriteAllText(EndpointsPath, JsonSerializer.Serialize(updated, Indented));
    }

    private static string? DetectBaseUrl(Dictionary<string, string> endpoints)
    {
        foreach (var url in endpoints.Values)
        {
            if (string.IsNullOrEmpty(url))
                continue;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}";
        }
        return null;
    }

    private static AppConfig CreateDefaultConfig()
    {
        return new AppConfig
        {
            BaseUrl = "http://localhost:5000",
            DeveloperUserIds = new List<string>(),
            VModUserIds = new List<string>(),
            AutoFillRROs = true,
            AutoFillStorefronts = true,
            WhitelistOn = false,
            WhitelistedPlatformIds = new List<string>(),
            EnableAccountCreation = true,
        };
    }

    public class AppConfig
    {
        public string BaseUrl { get; set; } = "";
        public bool AutoFillRROs { get; set; } = true;
        public bool AutoFillStorefronts { get; set; } = true;
        public List<string> DeveloperUserIds { get; set; } = new();
        public List<string> VModUserIds { get; set; } = new();
        public List<string> AdminAccountIds { get; set; } = new();
        public bool WhitelistOn { get; set; } = false;
        public List<string> WhitelistedPlatformIds { get; set; } = new();
        public bool EnableAccountCreation { get; set; } = true;
    }
}
