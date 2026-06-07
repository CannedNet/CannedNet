using CannedNet.Data;
using CannedNet.Hubs;
using CannedNet.Services;
using CannedNet.Services.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CannedNet;

public static class Program
{
    public static async Task Main(string[] args) {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        //builder.Services.AddLogging(logging => {
        //    logging.AddConsole();
        //    logging.SetMinimumLevel(LogLevel.Debug);
        //});
        
        string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Port=5432;Database=cannednet;Username=postgres;Password=postgres";
        
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        
        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.PropertyNamingPolicy = null);
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddScoped<StorefrontFillService>();
        builder.Services.AddScoped<JwtTokenService>();
        builder.Services.AddSignalR();

        builder.Services.AddAuthentication(options => 
            { 
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var rsa = Signatures.GetRsaInstance();
                var securityKey = new RsaSecurityKey(rsa) { KeyId = "7C2F041398671515B0862CB23FAF95B03" };

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateIssuer = true,
                    ValidIssuer = "https://lapis.codes",
                    ValidateAudience = true,
                    ValidAudience = "https://lapis.codes/resources",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
        
                    // sub is account id
                    NameClaimType = "sub",
                    RoleClaimType = "role" 
                }; 
            });
        
        builder.Services.AddAuthorization();
        
        WebApplication app = builder.Build();
        
        app.MapHub<NotificationsHub>("/hub/v1");
        app.UseHttpsRedirection();
        //app.UseRequestLogging();
        app.MapControllers();
        app.UseAuthentication();
        app.UseAuthorization();
        
        IHubContext<NotificationsHub> hubContext = app.Services.GetRequiredService<IHubContext<NotificationsHub>>();
        NotificationService.SetHubContext(hubContext);

        //if (app.Environment.IsDevelopment()) {
            app.UseSwagger();
            app.UseSwaggerUI(options => {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "CannedNet v1");
                options.RoutePrefix = "swagger/ui/";
            });
        //}
        
        using IServiceScope scope = app.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        StorefrontFillService seedingService = scope.ServiceProvider.GetRequiredService<StorefrontFillService>();
        await seedingService.FillStorefrontsAsync();
        
        try {
            await db.Database.MigrateAsync();
        } catch {
            await db.Database.EnsureCreatedAsync();
        }

        await app.RunAsync();
    }

    //public static async Task T() {
    //    List<(WebApplication App, ServiceRegistry Service)> apps = new();
    //
    //    foreach (var service in Services.Infrastructure.Services.All)
    //    {
    //        var builder = WebApplication.CreateBuilder();
    //
    //        builder.Services.AddLogging(logging =>
    //        {
    //            logging.AddConsole();
    //            logging.SetMinimumLevel(LogLevel.Debug);
    //        });
    //
    //        service.ConfigureBuilder(builder);
    //        builder.WebHost.UseUrls($"http://*:{service.Port}");
    //        builder.Configuration.AddJsonFile($"AppConfigs/appsettings.{service.Name}.json", optional: true, reloadOnChange: true);
    //
    //        apps.Add((builder.Build(), service));
    //    }
    //
    //    using var scope = apps[0].App.Services.CreateScope();
    //    var dbContext = scope.ServiceProvider.GetRequiredService<CannedNet.Data.AppDbContext>();
    //    try
    //    {
    //        dbContext.Database.Migrate();
    //    }
    //    catch (Exception ex)
    //    {
    //        dbContext.Database.EnsureCreated();
    //    }
    //
    //    // automatically fill out storefronts tables from the JSON's
    //    var seedingService = scope.ServiceProvider.GetRequiredService<StorefrontFillService>();
    //    await seedingService.FillStorefrontsAsync();
    //
    //    var jwtService = scope.ServiceProvider.GetRequiredService<JwtTokenService>();
    //
    //    foreach (var (app, service) in apps)
    //    {
    //        service.MapEndpoints(app, jwtService);
    //        app.UseRequestLogging();
    //    }
    //
    //    await Task.WhenAll(apps.Select(t => t.App.RunAsync()));
    //}
}
