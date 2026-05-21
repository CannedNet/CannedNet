using System.Net.Mime;
using CannedNet.Services.Infrastructure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace CannedNet.Services.Controllers;

public class ImageController
{
    private static readonly HttpClient HttpClient = new();

    public WebApplicationBuilder Initialize(string[]? args = null)
        => ServiceExtensions.CreateRecNetBuilder(args);

    public void MapEndpoints(WebApplication app)
    {
        var imagesDir = Path.Combine("Images");

        Directory.CreateDirectory(imagesDir);

        app.MapGet("/{imageName}", async (
            HttpContext context,
            string imageName
        ) =>
        {
            byte[] imageBytes;

            var filePath = Path.Combine(imagesDir, imageName);

            if (File.Exists(filePath))
            {
                imageBytes = await File.ReadAllBytesAsync(filePath);
            }
            else
            {
                var response = await HttpClient.GetAsync(
                    $"https://cdn.rec.net/img/{imageName}"
                );

                if (!response.IsSuccessStatusCode)
                    return Results.NotFound();

                imageBytes = await response.Content.ReadAsByteArrayAsync();
            }

            int? width = null;
            int? height = null;

            if (int.TryParse(context.Request.Query["width"], out var w))
                width = w;

            if (int.TryParse(context.Request.Query["height"], out var h))
                height = h;

            if (width == null && height == null)
            {
                return Results.File(imageBytes, MediaTypeNames.Image.Png);
            }

            using var image = Image.Load(imageBytes);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(
                    width ?? 0,
                    height ?? 0
                ),
                Mode = ResizeMode.Max
            }));

            await using var output = new MemoryStream();

            await image.SaveAsync(output, new PngEncoder());

            return Results.File(
                output.ToArray(),
                MediaTypeNames.Image.Png
            );
        });
    }
}