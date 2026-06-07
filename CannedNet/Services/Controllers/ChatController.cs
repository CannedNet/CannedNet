using Microsoft.AspNetCore.Mvc;

namespace CannedNet.Services.Controllers;

[ApiController]
public class ChatController : ControllerBase
{
    [HttpGet("thread")]
    public async Task<IResult> Get() {
        return Results.Content("[]", "application/json");
        
    }
}
