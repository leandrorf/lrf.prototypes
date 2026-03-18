using identityserver.web.Configuration;
using identityserver.web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace identityserver.web.Controllers;

public class AccountController : Controller
{
    private readonly IdentityServerOptions _identityServer;

    public AccountController(IOptions<IdentityServerOptions> identityServer)
    {
        _identityServer = identityServer.Value;
    }

    /// <summary>Exibe o formulário de login OAuth. Parâmetros vêm do IdentityServer (redirect) ou da query string.</summary>
    [HttpGet]
    public IActionResult SignIn(
        [FromQuery] string? client_id,
        [FromQuery] string? redirect_uri,
        [FromQuery] string? state,
        [FromQuery] string? scope,
        [FromQuery] string? code_challenge,
        [FromQuery] string? code_challenge_method,
        [FromQuery] string? error)
    {
        var model = new SignInViewModel
        {
            PostUrl = _identityServer.AuthorizeEndpoint,
            Error = error,
            ClientId = client_id ?? "",
            RedirectUri = redirect_uri ?? "",
            State = state ?? "",
            Scope = scope ?? "openid",
            CodeChallenge = code_challenge,
            CodeChallengeMethod = code_challenge_method ?? "S256"
        };
        return View(model);
    }
}
