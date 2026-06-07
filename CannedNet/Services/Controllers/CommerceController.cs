using Microsoft.AspNetCore.Mvc;

namespace CannedNet.Services.Controllers;

[ApiController, Route("commerce")]
public class CommerceController : ControllerBase
{
    [HttpGet("purchase/v1/hasspentmoney")]
    public async Task<IResult> HasSpentMoney()
    {
        return Results.NotFound();
    }
}
