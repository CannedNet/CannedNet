using CannedNet.Services.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CannedNet.Services.Controllers;

[ApiController, Route("img")]
public class ImageController : ControllerBase
{
    private static readonly byte[] PlaceholderJpeg;

    static ImageController()
    {
        Signatures.Init();

        using Image<Rgba32> image = new(64, 64);
        image.Mutate(x => x.BackgroundColor(Color.FromRgb(32, 32, 32)));
        using MemoryStream ms = new();
        image.SaveAsJpeg(ms, new JpegEncoder { Quality = 50 });
        PlaceholderJpeg = ms.ToArray();
    }

    [HttpGet("{imageName}")]
    public async Task<IResult> GetImage(string imageName)
    {
        HttpContext context = HttpContext;
        string imagesDir = Path.Combine("Images");
        Directory.CreateDirectory(imagesDir);
        string filePath = Path.Combine(imagesDir, imageName);

        byte[] imageBytes;
        string contentType;

        if (System.IO.File.Exists(filePath))
        {
            imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            string ext = Path.GetExtension(imageName).ToLowerInvariant();
            contentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "image/png"
            };
        }
        else
        {
            imageBytes = PlaceholderJpeg;
            contentType = "image/jpeg";
        }

        string? cropSquare = context.Request.Query["cropSquare"].FirstOrDefault();
        string? widthStr = context.Request.Query["width"].FirstOrDefault();
        string? heightStr = context.Request.Query["height"].FirstOrDefault();

        if (string.IsNullOrEmpty(widthStr) && string.IsNullOrEmpty(heightStr) && string.IsNullOrEmpty(cropSquare))
        {
            string? signature1 = Signatures.Sign(imageBytes);
            if (signature1 != null)
            {
                context.Response.Headers["Content-Signature"] = $"key-id=KEY:RSA:p1.rec.net; data={signature1}";
            }
            return Results.File(imageBytes, contentType);
        }

        using Image image = Image.Load(imageBytes);

        int resizeWidth = 0;
        int resizeHeight = 0;

        if (!string.IsNullOrEmpty(cropSquare) && cropSquare != "0" && cropSquare != "false")
        {
            int size = Math.Min(image.Width, image.Height);
            int x = (image.Width - size) / 2;
            int y = (image.Height - size) / 2;
            image.Mutate(img => img.Crop(new Rectangle(x, y, size, size)));
        }

        if (int.TryParse(widthStr, out int w))
            resizeWidth = w;

        if (int.TryParse(heightStr, out int h))
            resizeHeight = h;

        if (resizeWidth > 0 || resizeHeight > 0)
        {
            image.Mutate(x => x.Resize(resizeWidth, resizeHeight));
        }

        using MemoryStream output = new();
        await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 85 });
        imageBytes = output.ToArray();
        string? signature = Signatures.Sign(imageBytes);
        if (signature != null)
        {
            context.Response.Headers["Content-Signature"] = $"key-id=KEY:RSA:p1.rec.net; data={signature}";
        }
        return Results.File(imageBytes, "image/jpeg");
    }
}
