using CannedNet.Services.Infrastructure;

namespace CannedNet.Services.Controllers;

public class CDNController
{
    public WebApplicationBuilder Initialize(string[]? args = null) => ServiceExtensions.CreateRecNetBuilder(args);

    public void MapEndpoints(WebApplication app)
    {
        app.MapGet("/config/LoadingScreenTipData", (HttpRequest request) =>
        {
            var json = File.ReadAllText("JSON/loadingscreentipdata.json");
            return Results.Content(json, "application/json");
        });

        app.MapGet("/sigs/{sigName}", async (HttpContext context, string sigName) =>
        {
            var filePath = Path.Combine("Sigs", sigName);

            if (File.Exists(filePath))
            {
                var sigBytes = await File.ReadAllBytesAsync(filePath);
                return Results.File(sigBytes, "application/octet-stream");
            }
            else
            {
                return Results.NotFound();
            }
        });
        app.MapPost("/upload", async (HttpContext context) =>
        {
            try
            {
                var form = await context.Request.ReadFormAsync();
                var file = form.Files.FirstOrDefault();

                if (file == null)
                {
                    return Results.BadRequest(new { error = "No file found in request" });
                }

                var imageId = Guid.NewGuid().ToString("N");
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                var validExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp" };
                if (string.IsNullOrEmpty(extension) || !validExtensions.Contains(extension))
                {
                    extension = ".png";
                }

                var savedFileName = imageId + extension;
                var filePath = Path.Combine("Images", savedFileName);

                if (!Directory.Exists("Images"))
                {
                    Directory.CreateDirectory("Images");
                }

                using (var fileStream = File.Create(filePath))
                {
                    await file.CopyToAsync(fileStream);
                }

                return Results.Ok(new
                {
                    filename = savedFileName
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error uploading image: {ex.Message}");
            }
        });
    }
}