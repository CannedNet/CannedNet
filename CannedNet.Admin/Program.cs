using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CannedNet.Admin.Components;
using CannedNet.Admin.Services;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthentication("AdminCookie")
    .AddCookie("AdminCookie", options =>
    {
        options.Cookie.Name = "CannedNetAdmin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/login";
        options.LogoutPath = "/admin/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<AdminConfigService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<AdminConfigService>();
    return new ImageUrlService(config.ImagesBaseUrl);
});


builder.Services.AddTransient<JwtBearerHandler>();
builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<AdminConfigService>();
    var handler = sp.GetRequiredService<JwtBearerHandler>();
    handler.InnerHandler = new SocketsHttpHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(config.ServerBaseUrl) };
});

var app = builder.Build();

app.MapPost("/admin/login", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();
    var username = form["username"].FirstOrDefault() ?? "";
    var password = form["password"].FirstOrDefault() ?? "";

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        context.Response.Redirect("/login?error=missing");
        return;
    }

    var adminConfig = app.Services.GetRequiredService<AdminConfigService>();
    using var httpClient = new HttpClient { BaseAddress = new Uri(adminConfig.ServerBaseUrl) };
    var tokenResponse = await httpClient.PostAsync("/auth/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["grant_type"] = "password",
        ["username"] = username,
        ["password"] = password
    }));

    if (!tokenResponse.IsSuccessStatusCode)
    {
        context.Response.Redirect("/login?error=invalid");
        return;
    }

    var json = await tokenResponse.Content.ReadAsStringAsync();
    using var doc = System.Text.Json.JsonDocument.Parse(json);
    var accessToken = doc.RootElement.GetProperty("access_token").GetString();

    if (string.IsNullOrEmpty(accessToken))
    {
        context.Response.Redirect("/login?error=invalid");
        return;
    }

    var payloadJson = DecodeJwtPayload(accessToken);
    using var payload = JsonDocument.Parse(payloadJson);
    var accountId = payload.RootElement.TryGetProperty("sub", out var sub) ? sub.GetString() ?? "" : "";
    var role = "";
    if (payload.RootElement.TryGetProperty("role", out var r))
    {
        if (r.ValueKind == JsonValueKind.String)
            role = r.GetString() ?? "";
        else if (r.ValueKind == JsonValueKind.Array)
            role = string.Join(",", r.EnumerateArray().Select(x => x.GetString() ?? ""));
    }
    var name = payload.RootElement.TryGetProperty("platform_id", out var pid) ? pid.GetString() ?? username : username;

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, accountId),
        new(ClaimTypes.Name, name),
        new("access_token", accessToken)
    };

    if (role.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(rr => rr == "developer" || rr == "admin"))
        claims.Add(new Claim(ClaimTypes.Role, "Admin"));

    var identity = new ClaimsIdentity(claims, "AdminCookie");
    var principal = new ClaimsPrincipal(identity);

    await context.SignInAsync("AdminCookie", principal, new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTime.UtcNow.AddHours(8)
    });

    context.Response.Redirect("/");
});

app.MapGet("/admin/logout", async (HttpContext context) =>
{
    await context.SignOutAsync("AdminCookie");
    context.Response.Redirect("/login");
});

app.Use(async (context, next) =>
{
    var config = context.RequestServices.GetRequiredService<AdminConfigService>();
    if (!config.IsConfigured && !context.Request.Path.StartsWithSegments("/setup"))
    {
        var accept = context.Request.Headers.Accept.FirstOrDefault() ?? "";
        if (accept.Contains("text/html"))
        {
            context.Response.Redirect("/setup");
            return;
        }
    }
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string DecodeJwtPayload(string jwt)
{
    var parts = jwt.Split('.');
    if (parts.Length < 2) return "{}";
    var payload = parts[1];
    switch (payload.Length % 4)
    {
        case 2: payload += "=="; break;
        case 3: payload += "="; break;
    }
    var bytes = Convert.FromBase64String(payload);
    return Encoding.UTF8.GetString(bytes);
}
