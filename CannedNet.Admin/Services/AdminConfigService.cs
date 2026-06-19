using System.Text.Json;

namespace CannedNet.Admin.Services;

public class AdminConfigService
{
    private const string ConfigFile = "admin_config.json";
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public AdminConfigData Config { get; private set; }

    public string ServerBaseUrl => Config.ServerBaseUrl;
    public string ImagesBaseUrl => Config.ServerBaseUrl.TrimEnd('/') + "/img";

    public AdminConfigService()
    {
        Config = Load();
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Config.ServerBaseUrl);

    private static AdminConfigData Load()
    {
        if (!File.Exists(ConfigFile))
        {
            var empty = new AdminConfigData();
            File.WriteAllText(ConfigFile, JsonSerializer.Serialize(empty, Indented));
            return empty;
        }

        var json = File.ReadAllText(ConfigFile);
        return JsonSerializer.Deserialize<AdminConfigData>(json) ?? new AdminConfigData();
    }

    public void Save(AdminConfigData updated)
    {
        Config = updated;
        File.WriteAllText(ConfigFile, JsonSerializer.Serialize(Config, Indented));
    }

    public class AdminConfigData
    {
        public string ServerBaseUrl { get; set; } = "";
        public string NsBaseUrl { get; set; } = "";
    }
}
