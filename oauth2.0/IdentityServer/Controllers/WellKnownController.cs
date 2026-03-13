using IdentityServer.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IdentityServer.Controllers;

/// <summary>OpenID Connect Discovery (RFC 7662).</summary>
[Route(".well-known")]
[ApiController]
public class WellKnownController : ControllerBase
{
    private readonly JwtSettings _jwtSettings;

    public WellKnownController(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    [HttpGet("openid-configuration")]
    [Produces("application/json")]
    public IActionResult OpenIdConfiguration()
    {
        var issuer = _jwtSettings.Issuer.TrimEnd('/');
        var doc = new
        {
            issuer,
            authorization_endpoint = $"{issuer}/connect/authorize",
            token_endpoint = $"{issuer}/connect/token",
            userinfo_endpoint = $"{issuer}/connect/userinfo",
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code", "refresh_token" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "HS256" },
            scopes_supported = new[] { "openid" }
        };
        return Ok(doc);
    }
}
