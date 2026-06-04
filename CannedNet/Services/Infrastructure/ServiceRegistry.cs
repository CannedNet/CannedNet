using System.Net.Mime;
using CannedNet.Services.Controllers;

namespace CannedNet.Services.Infrastructure;

public class ServiceRegistry
{
    public required string Name { get; init; }
    public required int Port { get; init; }
    public required Type ServiceType { get; init; }
    public required Action<WebApplicationBuilder> ConfigureBuilder { get; init; }
    public required Action<WebApplication, JwtTokenService> MapEndpoints { get; init; }
}

public static class Services
{
    public static readonly List<ServiceRegistry> All =
    [
        new() { Name = "NS", Port = 5001, ServiceType = typeof(NSController), ConfigureBuilder = b => b.ConfigureRecNetServices(), MapEndpoints = (app, _) => new NSController().MapEndpoints(app) },
        new() { Name = "API", Port = 5000, ServiceType = typeof(APIController), ConfigureBuilder = b => b.ConfigureRecNetServices(), MapEndpoints = (app, _) => new APIController().MapEndpoints(app) },
        new() { Name = "Auth", Port = 5002, ServiceType = typeof(AuthController), ConfigureBuilder = b => b.ConfigureRecNetServices(), MapEndpoints = (app, jwt) => new AuthController().MapEndpoints(app) },
        new() { Name = "Chat", Port = 5003, ServiceType = typeof(ChatController), ConfigureBuilder = b => b.ConfigureRecNetServices(), MapEndpoints = (app, _) => new ChatController().MapEndpoints(app) },
        new() { Name = "Match", Port = 5004, ServiceType = typeof(MatchmakingController), ConfigureBuilder = b => b.ConfigureRecNetServices(), MapEndpoints = (app, _) => new MatchmakingController().MapEndpoints(app) },
        new() { Name = "Accounts", Port = 5005, ServiceType = typeof(AccountsController), ConfigureBuilder = b => b.ConfigureRecNetServices(), MapEndpoints = (app, jwt) => new AccountsController().MapEndpoints(app, jwt) },
        new() { Name = "Notify", Port = 5006, ServiceType = typeof(NotifyController), ConfigureBuilder = b => b.ConfigureRecNetServices(), MapEndpoints = (app, _) => new NotifyController().MapEndpoints(app) },
        new() { Name = "CDN", Port = 5007, ServiceType = typeof(CDNController), ConfigureBuilder = b => b.ConfigureRecNetServices(), MapEndpoints = (app, _) => new CDNController().MapEndpoints(app) },
        new() { Name = "Image", Port = 5008, ServiceType = typeof(ImageController), ConfigureBuilder = b => b.ConfigureRecNetServices(), MapEndpoints = (app, _) => new ImageController().MapEndpoints(app) },
        new() { Name = "Clubs", Port = 5009, ServiceType = typeof(ClubsController), ConfigureBuilder = b => b.ConfigureRecNetServices(), MapEndpoints = (app, _) => new ClubsController().MapEndpoints(app) },
        new() { Name = "Commerce", Port = 5010, ServiceType = typeof(CommerceController), ConfigureBuilder = b => b.ConfigureRecNetServices(), MapEndpoints = (app, _) => new CommerceController().MapEndpoints(app) },
        new() { Name = "Econ", Port = 5011, ServiceType = typeof(EconController), ConfigureBuilder = b => b.ConfigureRecNetServices(), MapEndpoints = (app, _) => new EconController().MapEndpoints(app) },
        new() { Name = "PlayerSettings", Port = 5012, ServiceType = typeof(PlayerSettingsController), ConfigureBuilder = b => b.ConfigureRecNetServices(), MapEndpoints = (app, _) => new PlayerSettingsController().MapEndpoints(app) },
        new() { Name = "RoomsController", Port = 5013, ServiceType = typeof(RoomsController), ConfigureBuilder = b => b.ConfigureRecNetServices(), MapEndpoints = (app, _) => new RoomsController().MapEndpoints(app) }
    ];
}
