using System.Security.Claims;
using System.Text.Json.Serialization;
using CannedNet.Data;
using CannedNet.Hubs;
using CannedNet.Services;
using CannedNet.Services.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CannedNet;

public static class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        //builder.Services.AddLogging(logging => {
        //    logging.AddConsole();
        //    logging.SetMinimumLevel(LogLevel.Debug);
        //});

        string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Port=5432;Database=cannednet;Username=postgres;Password=postgres";

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();


        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = null;
            options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddScoped<StorefrontFillService>();
        builder.Services.AddScoped<JwtTokenService>();
        builder.Services.AddSingleton<ConfigService>();
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

                    ValidateIssuer = false,
                    ValidIssuer = "https://lapis.codes",

                    ValidateAudience = false,
                    ValidAudiences = new[]
                    {
                        "https://lapis.codes",
                        "https://lapis.codes/resources"
                    },

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,

                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };
            })
            .AddScheme<AuthenticationSchemeOptions, AdminApiKeyHandler>("AdminApiKey", null);

        builder.Services.AddAuthorization();

        WebApplication app = builder.Build();

        app.MapHub<NotificationsHub>("/hub/v1");
        app.UseHttpsRedirection();
        app.UseRequestLogging();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        IHubContext<NotificationsHub> hubContext = app.Services.GetRequiredService<IHubContext<NotificationsHub>>();
        NotificationService.SetHubContext(hubContext);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "CannedNet v1");
                options.RoutePrefix = "swagger/ui/";
            });
        }

        using IServiceScope scope = app.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            await db.Database.MigrateAsync();
        }
        catch
        {
            await db.Database.EnsureCreatedAsync();
        }

        StorefrontFillService seedingService = scope.ServiceProvider.GetRequiredService<StorefrontFillService>();
        await seedingService.FillStorefrontsAsync();

        Signatures.Init();

        await app.RunAsync();
    }
}
