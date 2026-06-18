using Microsoft.AspNetCore.Mvc;

namespace CannedNet.Services.Controllers;

[ApiController, Route("ns")]
public class NSController : ControllerBase
{
    [HttpGet]
    public async Task<IResult> Get()
    {
        string json = await System.IO.File.ReadAllTextAsync("JSON/endpoints.json");
        return Results.Content(json, "application/json");
    }
}
