using System.Net.Http.Json;
using identityserver.web.Configuration;
using identityserver.web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace identityserver.web.Controllers;

public class AccountController : Controller
{
    private readonly IdentityServerOptions _identityServer;
    private readonly IHttpClientFactory _httpClientFactory;

    public AccountController(IOptions<IdentityServerOptions> identityServer, IHttpClientFactory httpClientFactory)
    {
        _identityServer = identityServer.Value;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>Página de login que retorna os tokens (access_token, refresh_token, id_token) após autenticar.</summary>
    [HttpGet]
    public IActionResult LoginToken()
    {
        return View(new LoginTokenViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginToken(string? userName, string? password, CancellationToken cancellationToken)
    {
        var model = new LoginTokenViewModel { UserName = userName };
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            model.Error = "Usuário e senha são obrigatórios.";
            return View(model);
        }

        var client = _httpClientFactory.CreateClient("IdentityServer");
        var body = new { userName = userName.Trim(), password };
        var response = await client.PostAsJsonAsync("api/Auth/login", body, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            model.Error = response.StatusCode == System.Net.HttpStatusCode.BadRequest
                ? "Usuário ou senha inválidos."
                : "Erro ao autenticar. Tente novamente.";
            return View(model);
        }

        var result = await response.Content.ReadFromJsonAsync<LoginTokenResult>(cancellationToken);
        if (result is null)
        {
            model.Error = "Resposta inválida da API.";
            return View(model);
        }

        model.AccessToken = result.access_token;
        model.RefreshToken = result.refresh_token;
        model.IdToken = result.id_token;
        model.TokenType = result.token_type ?? "Bearer";
        model.ExpiresIn = result.expires_in;
        return View(model);
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

    private sealed class LoginTokenResult
    {
        public string? access_token { get; set; }
        public string? refresh_token { get; set; }
        public string? id_token { get; set; }
        public string? token_type { get; set; }
        public int? expires_in { get; set; }
    }
}
