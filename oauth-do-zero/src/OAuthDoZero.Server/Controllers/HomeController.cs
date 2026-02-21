using Microsoft.AspNetCore.Mvc;

namespace OAuthDoZero.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { name = "OAuth do Zero", version = "0.1" });
    }
}
