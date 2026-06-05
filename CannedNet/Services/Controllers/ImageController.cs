using System.Net.Mime;
using CannedNet.Services.Infrastructure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CannedNet.Services.Controllers;

public class ImageController
{
    private static readonly byte[] PlaceholderJpeg;

    static ImageController()
    {
        Signatures.Init();

        using var image = new Image<Rgba32>(64, 64);
        image.Mutate(x => x.BackgroundColor(Color.FromRgb(32, 32, 32)));
        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms, new JpegEncoder { Quality = 50 });
        PlaceholderJpeg = ms.ToArray();
    }

    public WebApplicationBuilder Initialize(string[]? args = null)
        => ServiceExtensions.CreateRecNetBuilder(args);

    public void MapEndpoints(WebApplication app)
    {
        var imagesDir = Path.Combine("Images");
        Directory.CreateDirectory(imagesDir);

        app.MapGet("/{imageName}", async (HttpContext context, string imageName) =>
        {
            var filePath = Path.Combine(imagesDir, imageName);

            byte[] imageBytes;
            string contentType;

            if (File.Exists(filePath))
            {
                imageBytes = await File.ReadAllBytesAsync(filePath);
                var ext = Path.GetExtension(imageName).ToLowerInvariant();
                contentType = ext switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    ".bmp" => "image/bmp",
                    _ => "image/png"
                };
            }
            else
            {
                imageBytes = PlaceholderJpeg;
                contentType = "image/jpeg";
            }

            var cropSquare = context.Request.Query["cropSquare"].FirstOrDefault();
            var widthStr = context.Request.Query["width"].FirstOrDefault();
            var heightStr = context.Request.Query["height"].FirstOrDefault();

            if (string.IsNullOrEmpty(widthStr) && string.IsNullOrEmpty(heightStr) && string.IsNullOrEmpty(cropSquare))
            {
                SignImageResponse(context, ref imageBytes);
                return Results.File(imageBytes, contentType);
            }

            using var image = Image.Load(imageBytes);

            var resizeWidth = 0;
            var resizeHeight = 0;

            if (!string.IsNullOrEmpty(cropSquare) && cropSquare != "0" && cropSquare != "false")
            {
                var size = Math.Min(image.Width, image.Height);
                var x = (image.Width - size) / 2;
                var y = (image.Height - size) / 2;
                image.Mutate(img => img.Crop(new Rectangle(x, y, size, size)));
            }

            if (int.TryParse(widthStr, out var w))
                resizeWidth = w;

            if (int.TryParse(heightStr, out var h))
                resizeHeight = h;

            if (resizeWidth > 0 || resizeHeight > 0)
            {
                image.Mutate(x => x.Resize(resizeWidth, resizeHeight));
            }

            using var output = new MemoryStream();
            await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 85 });
            imageBytes = output.ToArray();
            SignImageResponse(context, ref imageBytes);
            return Results.File(imageBytes, "image/jpeg");
        });
    }

    private static void SignImageResponse(HttpContext context, ref byte[] imageBytes)
    {
        if (context.Request.Query["sig"] != "p1") return;

        var signature = Signatures.Sign(imageBytes);
        if (signature != null)
        {
            context.Response.Headers["Content-Signature"] = $"key-id=KEY:RSA:p1.rec.net; data={signature}";
        }
    }
}