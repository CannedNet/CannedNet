using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using CannedNet.Services;

namespace CannedNet;

public class JwtTokenService
{
    private static readonly RSA _rsa = RSA.Create(2048);
    private readonly RsaSecurityKey _securityKey;
    private readonly SigningCredentials _signingCredentials;
    private const string KeyId = "7C2F041398671515B0862CB23FAF95B03";

    public JwtTokenService()
    {
        var rsa = Signatures.GetRsaInstance()
                  ?? throw new InvalidOperationException("jwt - sigs service was not found");

        _securityKey = new RsaSecurityKey(rsa) { KeyId = KeyId };

        _signingCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.RsaSha256);
    }

    public string GenerateToken(string accountId, string platformId, ConfigService config, string platform = "Steam", List<string>? additionalRoles = null, string amr = "cached_login")
    {
        var now = DateTime.UtcNow;
        var exp = now.AddHours(1);

        // theres gotta be some other better way to do this, but im fucking stupid so :shrug:
        // if it works it works ig idk, someone can clean this up later or i will later

        var claims = new List<Claim>
        {
            new Claim("iss", "https://lapis.codes"),
            new Claim("nbf", ((DateTimeOffset)now).ToUnixTimeSeconds().ToString()),
            new Claim("iat", ((DateTimeOffset)now).ToUnixTimeSeconds().ToString()),
            new Claim("exp", ((DateTimeOffset)exp).ToUnixTimeSeconds().ToString()),
            new Claim("aud", "https://lapis.codes/resources"),
            new Claim("amr", amr),
            new Claim("client_id", "recroom"),
            new Claim("sub", accountId),
            new Claim("auth_time", ((DateTimeOffset)now).ToUnixTimeSeconds().ToString()),
            new Claim("idp", "local"),
            new Claim("platform", platform),
            new Claim("platform_id", platformId),
            new Claim("rn.ver", "20210129"),
            new Claim("rn.plat", "0"),
            new Claim("role", "gameClient"),
            new Claim("jti", Guid.NewGuid().ToString("N")[..32].ToUpper())
        };

        if (config.Config.DeveloperUserIds.Contains(platformId))
        {
            claims.Add(new Claim("role", "developer"));
        }

        if (config.Config.VModUserIds.Contains(platformId))
        {
            claims.Add(new Claim("role", "moderator"));
        }

        if (additionalRoles != null)
        {
            foreach (var role in additionalRoles)
                claims.Add(new Claim("role", role));
        }

        claims.Add(new Claim("scope", "profile"));
        claims.Add(new Claim("scope", "rn"));
        claims.Add(new Claim("scope", "rn.accounts"));
        claims.Add(new Claim("scope", "rn.accounts.gc"));
        claims.Add(new Claim("scope", "rn.api"));
        claims.Add(new Claim("scope", "rn.chat"));
        claims.Add(new Claim("scope", "rn.clubs"));
        claims.Add(new Claim("scope", "rn.commerce"));
        claims.Add(new Claim("scope", "rn.match.read"));
        claims.Add(new Claim("scope", "rn.match.write"));
        claims.Add(new Claim("scope", "rn.notify"));
        claims.Add(new Claim("scope", "rn.rooms"));
        claims.Add(new Claim("scope", "rn.storage"));
        claims.Add(new Claim("scope", "offline_access"));

        var identity = new ClaimsIdentity(claims, "jwt");

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(
            issuer: "https://auth.lapis.codes",
            audience: "https://auth.lapis.codes/resources",
            subject: identity,
            notBefore: now,
            issuedAt: now,
            expires: exp,
            signingCredentials: _signingCredentials);

        token.Payload["amr"] = new List<string> { amr };

        return handler.WriteToken(token);
    }

    public string? ValidateAndGetAccountId(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            handler.InboundClaimTypeMap.Clear();

            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _securityKey,
                ValidateIssuer = true,
                ValidIssuer = "https://auth.lapis.codes",
                ValidateAudience = true,
                ValidAudience = "https://auth.lapis.codes/resources",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = handler.ValidateToken(token, parameters, out SecurityToken validatedToken);

            var accountIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "sub");

            return accountIdClaim?.Value;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public int? GetAccountIdFromContext(HttpContext context)
    {
        try
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return null;

            var token = authHeader.Substring("Bearer ".Length);
            var accountId = ValidateAndGetAccountId(token);

            if (string.IsNullOrEmpty(accountId) || !int.TryParse(accountId, out var id))
                return null;

            return id;
        }
        catch
        {
            return null;
        }
    }
}
