using Microsoft.AspNetCore.Mvc;

namespace CannedNet.Services.Controllers;

//TODO: proper NameServer configs
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
