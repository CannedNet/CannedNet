using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CannedNet.Services;

public class AdminApiKeyHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public AdminApiKeyHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var config = Context.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = config.GetValue<string>("Admin:ApiKey")
            ?? config.GetValue<string>("AdminApiKey")
            ?? "";

        string? apiKey = Request.Headers["X-Admin-Api-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey) || apiKey != expectedKey)
            return Task.FromResult(AuthenticateResult.Fail("Invalid or missing X-Admin-Api-Key header"));

        var claims = new[] { new Claim(ClaimTypes.Role, "Admin") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        await Results.Unauthorized().ExecuteAsync(Context);
    }
}
