using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CannedNet.Services.Controllers;

[ApiController, Route("club")]
public class ClubsController : ControllerBase
{
    [HttpGet("home/me")]
    [Authorize]
    public async Task<IResult> GetClubHomeMe()
    {
        return Results.NotFound();
    }
}