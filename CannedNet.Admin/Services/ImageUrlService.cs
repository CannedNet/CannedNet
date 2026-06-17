namespace CannedNet.Admin.Services;

public class ImageUrlService
{
    private readonly string _baseUrl;

    public ImageUrlService(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public string GetUrl(string? path)
    {
        if (string.IsNullOrEmpty(path)) return "";
        return $"{_baseUrl}/{path.TrimStart('/')}";
    }
}
