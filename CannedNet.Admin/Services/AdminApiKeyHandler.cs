namespace CannedNet.Admin.Services;

public class AdminApiKeyHandler : DelegatingHandler
{
    private readonly string _apiKey;

    public AdminApiKeyHandler(IConfiguration config)
    {
        _apiKey = config.GetValue<string>("Admin:ApiKey")
            ?? config.GetValue<string>("AdminApiKey")
            ?? "";
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_apiKey))
            request.Headers.Add("X-Admin-Api-Key", _apiKey);

        return await base.SendAsync(request, cancellationToken);
    }
}
