namespace CannedNet.Admin.Models;

public static class EndpointDefinitions
{
    public static readonly (string Name, string Path)[] Endpoints =
    [
        ("Accounts", "/accounts"),
        ("AI", "/ai"),
        ("API", "/api"),
        ("Auth", "/auth"),
        ("BugReporting", "/bugreporting"),
        ("Cards", "/cards"),
        ("CDN", "/cdn"),
        ("Chat", "/chat"),
        ("Clubs", "/clubs"),
        ("CMS", "/cms"),
        ("Commerce", "/commerce"),
        ("Data", "/data"),
        ("DataCollection", "/datacollection"),
        ("Discovery", "/discovery"),
        ("Econ", "/econ"),
        ("GameLogs", "/gamelogs"),
        ("Geo", "/geo"),
        ("Images", "/img"),
        ("Leaderboard", "/leaderboard"),
        ("Link", "/link"),
        ("Lists", "/lists"),
        ("Matchmaking", "/match"),
        ("Moderation", "/moderation"),
        ("Notifications", ""),
        ("PlatformNotifications", ""),
        ("PlayerSettings", "/playersettings"),
        ("RoomComments", "/roomcomments"),
        ("RoomieIntegrations", "/roomieintegrations"),
        ("Rooms", "/rooms"),
        ("Storage", "/storage"),
        ("Strings", "/strings"),
        ("StringsCDN", "/stringscdn"),
        ("Studio", "/studio"),
        ("Thorn", "/thorn"),
        ("Videos", "/videos"),
        ("WWW", ""),
    ];

    public static string ExtractBaseUrl(string json)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        foreach (var (name, path) in Endpoints)
        {
            if (!string.IsNullOrEmpty(path) && root.TryGetProperty(name, out var urlEl))
            {
                var fullUrl = urlEl.GetString();
                if (fullUrl != null && fullUrl.EndsWith(path))
                    return fullUrl[..^path.Length].TrimEnd('/');
            }
        }

        foreach (var (name, _) in Endpoints)
        {
            if (root.TryGetProperty(name, out var urlEl))
            {
                var fullUrl = urlEl.GetString();
                if (!string.IsNullOrEmpty(fullUrl) && Uri.TryCreate(fullUrl, UriKind.Absolute, out var uri))
                    return $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}";
            }
        }

        return "";
    }

    public static string BuildEndpointsJson(string baseUrl)
    {
        var clean = baseUrl.TrimEnd('/');
        var dict = new Dictionary<string, string>();
        foreach (var (name, path) in Endpoints)
            dict[name] = clean + path;
        return System.Text.Json.JsonSerializer.Serialize(dict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
