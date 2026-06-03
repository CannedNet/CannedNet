using System.Text;

namespace CannedNet.Services.Infrastructure;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        var request = context.Request;
        var requestId = context.TraceIdentifier;
        
        string body = "";
        if (request.ContentLength > 0 && request.ContentLength < 10000)
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        var originalBody = context.Response.Body;
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            
            var headers = string.Join("\n  ", request.Headers
                .Where(h => !IsSensitiveHeader(h.Key))
                .Select(h => $"{h.Key}: {h.Value}"));
            
            var sensitiveHeaders = string.Join("\n  ", request.Headers
                .Where(h => IsSensitiveHeader(h.Key))
                .Select(h => $"{h.Key}: [REDACTED]"));
            
            var headerStr = string.IsNullOrEmpty(sensitiveHeaders) ? headers : $"{headers}\n  {sensitiveHeaders}";
            
            var responseBodyText = "";
            if (responseBody.Length > 0 && responseBody.Length < 10000)
            {
                responseBody.Position = 0;
                using var responseReader = new StreamReader(responseBody, Encoding.UTF8, leaveOpen: true);
                responseBodyText = responseReader.ReadToEnd();
                responseBody.Position = 0;
            }

            if (!string.IsNullOrWhiteSpace(body))
                _logger.LogInformation("[{RequestId}] {Method} {Path}{QueryString}\n  {HeaderStr}\n  Body: {Body}\n  Response: {StatusCode} {ResponseBody}",
                    requestId, request.Method, request.Path, request.QueryString, headerStr, body, context.Response.StatusCode, responseBodyText);
            else
                _logger.LogInformation("[{RequestId}] {Method} {Path}{QueryString}\n  {HeaderStr}\n  Response: {StatusCode} {ResponseBody}",
                    requestId, request.Method, request.Path, request.QueryString, headerStr, context.Response.StatusCode, responseBodyText);

            responseBody.Position = 0;
            await responseBody.CopyToAsync(originalBody);
            context.Response.Body = originalBody;
        }
    }

    private static bool IsSensitiveHeader(string headerName)
    {
        var sensitiveHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { };
        return sensitiveHeaders.Contains(headerName);
    }
}

public static class RequestLoggingExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
