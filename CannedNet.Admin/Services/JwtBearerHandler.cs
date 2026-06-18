using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;

namespace CannedNet.Admin.Services;

public class JwtBearerHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authStateProvider;

    public JwtBearerHandler(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        var token = state.User.FindFirst("access_token")?.Value;
        if (token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
