/*using CannedNet.Hubs;
using CannedNet.Services.Infrastructure;
using Microsoft.AspNetCore.SignalR;

namespace CannedNet.Services.Controllers;

public class NotifyController
{
    public WebApplicationBuilder Initialize(string[]? args = null) => ServiceExtensions.CreateRecNetBuilder(args);

    public void MapEndpoints(WebApplication app)
    {
        app.MapHub<NotificationsHub>("/hub/v1");

        var hubContext = app.Services.GetRequiredService<IHubContext<NotificationsHub>>();
        NotificationService.SetHubContext(hubContext);
    }
}
*/