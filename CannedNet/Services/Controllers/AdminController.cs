using Microsoft.AspNetCore.Mvc;

namespace CannedNet.Services.Controllers;

[ApiController, Route("admin")]
public class AdminController : ControllerBase
{
    [HttpGet]
    public async Task<IResult> GetRoot()
    {
        // TODO: allow getting admin panel from here
        return Results.NoContent();
    }
}
