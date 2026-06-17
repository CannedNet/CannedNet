using System.Security.Claims;
using CannedNet.Admin.Components;
using CannedNet.Admin.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OwnerOnly", policy => policy.RequireClaim("role", "Owner"));
});

builder.Services.AddSingleton<AdminAuthService>();
var imagesBaseUrl = builder.Configuration.GetValue<string>("ImagesBaseUrl") ?? "http://localhost:5000/img";
builder.Services.AddSingleton(new ImageUrlService(imagesBaseUrl));
builder.Services.AddHttpContextAccessor();


var serverUrl = builder.Configuration.GetValue<string>("ServerBaseUrl") ?? "http://localhost:5000";
builder.Services.AddTransient<AdminApiKeyHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AdminApiKeyHandler>();
    handler.InnerHandler = new SocketsHttpHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(serverUrl) };
});

var app = builder.Build();

// --- Admin Auth Endpoints ---

app.MapPost("/admin/login", async (HttpContext context, AdminAuthService authService) =>
{
    var form = await context.Request.ReadFormAsync();
    var username = form["username"].FirstOrDefault() ?? "";
    var password = form["password"].FirstOrDefault() ?? "";

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        context.Response.Redirect("/login?error=missing");
        return;
    }

    var user = authService.GetByUsername(username);
    if (user == null || !authService.VerifyPassword(password, user.PasswordHash))
    {
        context.Response.Redirect("/login?error=invalid");
        return;
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim("role", user.IsOwner ? "Owner" : "Admin")
    };

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

// --- Standard middleware ---

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
