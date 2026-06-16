using Microsoft.AspNetCore.Mvc;

namespace CannedNet.Services.Controllers;

[ApiController, Route("moderation")]
public class ModerationController : ControllerBase
{
    [HttpGet("voice/config")]
    public async Task<IResult> GetVoiceModConfig()
    {
        return Results.Ok(new
        {
            AccountId = (string?)null,
            ApiKey = (string?)null
        });
    }
}
