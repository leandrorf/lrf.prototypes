using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace identityserver.api.Controllers;

/// <summary>Exemplos de endpoints protegidos por permissão (claim "permission").</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DemoFeaturesController : ControllerBase
{
    [HttpGet("read")]
    [Authorize(Policy = "perm:app.demo.read")]
    public IActionResult Read() => Ok(new { message = "Leitura permitida (app.demo.read)." });

    [HttpGet("write")]
    [Authorize(Policy = "perm:app.demo.write")]
    public IActionResult Write() => Ok(new { message = "Escrita permitida (app.demo.write)." });
}
