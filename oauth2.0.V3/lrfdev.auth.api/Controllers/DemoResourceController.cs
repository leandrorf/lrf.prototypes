using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using lrfdev.auth.api.Authorization;

namespace lrfdev.auth.api.Controllers;

/// <summary>
/// Simula uma API recurso: exige Bearer JWT emitido por este servidor (usuário ou M2M).
/// </summary>
[ApiController]
[Route("api/demo")]
public sealed class DemoResourceController : ControllerBase
{
    [HttpGet("whoami")]
    [Authorize]
    public IActionResult WhoAmI()
    {
        var sub = User.FindFirst("sub")?.Value;
        var clientId = User.FindFirst("client_id")?.Value;
        var permissions = User.FindAll("permission").Select(c => c.Value).Distinct().ToArray();
        var scope = User.FindFirst("scope")?.Value;

        return Ok(new
        {
            subject = sub,
            client_id = clientId,
            permissions,
            scope
        });
    }

    [HttpGet("read")]
    [Authorize(Policy = "Scope:auth.read")]
    public IActionResult Read() =>
        Ok(new { message = "OK: exige escopo ou permissão auth.read" });

    /// <summary>Exige <c>auth.manage</c> no token (ex.: pedir <c>scope=auth.manage</c> no client_credentials).</summary>
    [HttpGet("manage")]
    [Authorize(Policy = "Scope:auth.manage")]
    public IActionResult Manage() =>
        Ok(new { message = "OK: exige escopo ou permissão auth.manage" });

    [HttpGet("orders")]
    [Authorize(Policy = "Scope:orders.read")]
    public IActionResult Orders() =>
        Ok(new { message = "OK: exige escopo ou permissão orders.read" });

    [HttpPost("orders")]
    [Authorize(Policy = "Scope:orders.write")]
    public IActionResult CreateOrder() =>
        Ok(new { message = "OK: exige escopo ou permissão orders.write" });
}
