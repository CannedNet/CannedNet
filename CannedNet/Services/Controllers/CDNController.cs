using CannedNet.Services.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CannedNet.Services.Controllers;

[ApiController, Route("cdn")]
public class CDNController
{
    [HttpGet("config/LoadingScreenTipData")]
    public Task<IResult> GetLoadingScreenTipData() {
        var json = File.ReadAllText("JSON/loadingscreentipdata.json");
        return Task.FromResult(Results.Content(json, "application/json"));
    }

    [HttpGet("sigs/{sigName}")]
    public async Task<IResult> GetSig(string sigName)
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
    }

    [HttpPost("upload")]
    public async Task<IResult> Upload(IFormFile file)
    {
        try
        {
            if (file == null)
            {
                return Results.BadRequest(new { error = "No file found in request" });
            }

            var imageId = Guid.NewGuid().ToString("N");
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
            var validExtensions = new[] { ".png", ".jpg", ".jpeg" };
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
    }
}