using System.Net.Http.Json;
using identityserver.web.Configuration;
using identityserver.web.Models;
using identityserver.web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace identityserver.web.Controllers;

public class AccountController : Controller
{
    private readonly IdentityServerOptions _identityServer;
    private readonly AppOptions _appOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITvSessionStore _tvSessionStore;

    public AccountController(
        IOptions<IdentityServerOptions> identityServer,
        IOptions<AppOptions> appOptions,
        IHttpClientFactory httpClientFactory,
        ITvSessionStore tvSessionStore)
    {
        _identityServer = identityServer.Value;
        _appOptions = appOptions.Value;
        _httpClientFactory = httpClientFactory;
        _tvSessionStore = tvSessionStore;
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

    /// <summary>TV: exibe QR code. Celular: ao escanear, exibe formulário de login. Após login, a TV recebe os tokens via polling.</summary>
    [HttpGet]
    public IActionResult TvLogin([FromQuery] string? session_id)
    {
        if (!string.IsNullOrWhiteSpace(session_id))
        {
            var result = _tvSessionStore.Get(session_id);
            if (result is null)
                return View("TvLoginExpired");
            return View("TvLoginScan", new TvLoginScanViewModel { SessionId = session_id });
        }
        var newSessionId = _tvSessionStore.CreateSession();
        var baseUrl = !string.IsNullOrWhiteSpace(_appOptions.PublicBaseUrl)
            ? _appOptions.PublicBaseUrl.TrimEnd('/')
            : $"{Request.Scheme}://{Request.Host}{Request.PathBase}".TrimEnd('/');
        var scanUrl = $"{baseUrl}/Account/TvLogin?session_id={newSessionId}";
        return View("TvLogin", new TvLoginViewModel { SessionId = newSessionId, ScanUrl = scanUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TvLogin(string? session_id, string? userName, string? password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(session_id))
            return RedirectToAction(nameof(TvLogin));
        var model = new TvLoginScanViewModel { SessionId = session_id, UserName = userName };
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            model.Error = "Usuário e senha são obrigatórios.";
            return View("TvLoginScan", model);
        }
        var client = _httpClientFactory.CreateClient("IdentityServer");
        var body = new { userName = userName.Trim(), password };
        var response = await client.PostAsJsonAsync("api/Auth/login", body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            model.Error = response.StatusCode == System.Net.HttpStatusCode.BadRequest ? "Usuário ou senha inválidos." : "Erro ao autenticar.";
            return View("TvLoginScan", model);
        }
        var result = await response.Content.ReadFromJsonAsync<LoginTokenResult>(cancellationToken);
        if (result is null)
        {
            model.Error = "Resposta inválida da API.";
            return View("TvLoginScan", model);
        }
        _tvSessionStore.SetTokens(session_id, new TvSessionTokens
        {
            AccessToken = result.access_token ?? "",
            RefreshToken = result.refresh_token ?? "",
            IdToken = result.id_token ?? "",
            TokenType = result.token_type ?? "Bearer",
            ExpiresIn = result.expires_in ?? 0
        });
        return View("TvLoginSuccess");
    }

    /// <summary>Polling da TV: retorna JSON com status (pending/completed) e tokens quando concluído.</summary>
    [HttpGet]
    public IActionResult TvLoginStatus([FromQuery] string? session_id)
    {
        if (string.IsNullOrWhiteSpace(session_id))
            return BadRequest(new { status = "error", error = "session_id obrigatório" });
        var result = _tvSessionStore.Get(session_id);
        if (result is null)
            return Ok(new { status = "expired" });
        return Ok(new
        {
            status = result.Status,
            access_token = result.AccessToken,
            refresh_token = result.RefreshToken,
            id_token = result.IdToken,
            token_type = result.TokenType,
            expires_in = result.ExpiresIn
        });
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
