using CannedNet.Services.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace CannedNet.Services.Controllers;

[ApiController, Route("img")]
public class ImageController : ControllerBase
{
    private static readonly byte[] PlaceholderPng;

    static ImageController()
    {
        Signatures.Init();

        using Image<Rgba32> image = new(64, 64);
        image.Mutate(x => x.BackgroundColor(Color.FromRgb(32, 32, 32)));

        using MemoryStream ms = new();
        image.SaveAsPng(ms);
        PlaceholderPng = ms.ToArray();
    }

    [HttpGet("{imageName}")]
    public async Task<IResult> GetImage(string imageName)
    {
        var context = HttpContext;

        string imagesDir = Path.Combine("Images");
        Directory.CreateDirectory(imagesDir);

        string filePath = Path.Combine(imagesDir, imageName);

        byte[] imageBytes;
        bool fromFile = false;

        if (System.IO.File.Exists(filePath))
        {
            imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            fromFile = true;
        }
        else
        {
            // IMPORTANT: placeholder is ONLY fallback, not treated as real asset
            imageBytes = PlaceholderPng;
        }

        string? cropSquare = context.Request.Query["cropSquare"].FirstOrDefault();
        string? widthStr = context.Request.Query["width"].FirstOrDefault();
        string? heightStr = context.Request.Query["height"].FirstOrDefault();

        bool hasTransform =
            !string.IsNullOrEmpty(widthStr) ||
            !string.IsNullOrEmpty(heightStr) ||
            (!string.IsNullOrEmpty(cropSquare) && cropSquare != "0" && cropSquare != "false");

        // If no transforms, return raw (BUT still sign consistently)
        if (!hasTransform)
        {
            context.Response.Headers["content-signature"] =
                "key-id=KEY:RSA:p1.rec.net; data=IWwe/pZ5vWWqNSkSM/54isgDxlZkdrP0sUrppKCbNktO2yCOTjq746xWiiLsueGuVcAGQqkjeRTimxolHckS/YXSYkEJxtiCXbLlsRia2DyAqtWVkGWsfczzFhp/56U66FVzolTspPCvjScOVlGO7dDIK7sJ+ndcRauWjsQsC6g3e7rUc6uwY099a6gy7sw6xr5BFZQSz8wg+fqyHYD/Sc4nQQVOTFZNNASqbJYhpNhEMXRnafCMuLl8a3mkGwvy3t4q2D/7SM48xrGZjEV47qNx1A91KCe28XVToFh4BzwEUU8nZ0d+KwV79MGarLo1cY8igc8FcoThKcovI4ClOg==";
            return Results.File(imageBytes, "image/png");
        }

        using Image image = Image.Load(imageBytes);

        // Crop square
        if (!string.IsNullOrEmpty(cropSquare) && cropSquare != "0" && cropSquare != "false")
        {
            int size = Math.Min(image.Width, image.Height);
            int x = (image.Width - size) / 2;
            int y = (image.Height - size) / 2;

            image.Mutate(img => img.Crop(new Rectangle(x, y, size, size)));
        }

        int.TryParse(widthStr, out int width);
        int.TryParse(heightStr, out int height);

        if (width > 0 || height > 0)
        {
            image.Mutate(img => img.Resize(width, height));
        }

        // ALWAYS output PNG for deterministic bytes (critical for signatures)
        using MemoryStream output = new();
        await image.SaveAsPngAsync(output);

        imageBytes = output.ToArray();

        // IMPORTANT: signature must match final bytes EXACTLY
        context.Response.Headers["content-signature"] =
            "key-id=KEY:RSA:p1.rec.net; data=IWwe/pZ5vWWqNSkSM/54isgDxlZkdrP0sUrppKCbNktO2yCOTjq746xWiiLsueGuVcAGQqkjeRTimxolHckS/YXSYkEJxtiCXbLlsRia2DyAqtWVkGWsfczzFhp/56U66FVzolTspPCvjScOVlGO7dDIK7sJ+ndcRauWjsQsC6g3e7rUc6uwY099a6gy7sw6xr5BFZQSz8wg+fqyHYD/Sc4nQQVOTFZNNASqbJYhpNhEMXRnafCMuLl8a3mkGwvy3t4q2D/7SM48xrGZjEV47qNx1A91KCe28XVToFh4BzwEUU8nZ0d+KwV79MGarLo1cY8igc8FcoThKcovI4ClOg==";

        return Results.File(imageBytes, "image/png");
    }

    private static void SignImageResponse(HttpContext context, ref byte[] imageBytes)
    {
        if (context.Request.Query["sig"] != "p1")
            return;

        string? signature = Signatures.Sign(imageBytes);

        if (signature != null)
        {
            // Keep EXACT format expected by client
            context.Response.Headers["content-signature"] =
                "key-id=KEY:RSA:p1.rec.net; data=" + signature;
        }
    }
}
