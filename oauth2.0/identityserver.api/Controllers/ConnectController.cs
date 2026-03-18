using identityserver.api.Configuration;
using identityserver.api.Data;
using identityserver.api.Models;
using identityserver.api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace identityserver.api.Controllers;

/// <summary>Endpoints OAuth 2.0 / OpenID Connect: authorize, token, revoke, userinfo.</summary>
[Route("connect")]
[ApiController]
public class ConnectController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IOAuthService _oauthService;
    private readonly JwtSettings _jwtSettings;
    private readonly LoginUiOptions _loginUi;

    public ConnectController(
        AuthDbContext db,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        IOAuthService oauthService,
        IOptions<JwtSettings> jwtSettings,
        IOptions<LoginUiOptions> loginUi)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _oauthService = oauthService;
        _jwtSettings = jwtSettings.Value;
        _loginUi = loginUi.Value;
    }

    /// <summary>GET: exibe formulário de login OAuth. Parâmetros: client_id, redirect_uri, response_type=code (opcional), state, scope (opcional), code_challenge e code_challenge_method (PKCE).</summary>
    [HttpGet("authorize")]
    [Produces("text/html")]
    public async Task<IActionResult> Authorize(
        [FromQuery] string? client_id,
        [FromQuery] string? redirect_uri,
        [FromQuery] string? response_type,
        [FromQuery] string? state,
        [FromQuery] string? scope,
        [FromQuery] string? code_challenge,
        [FromQuery] string? code_challenge_method,
        CancellationToken cancellationToken)
    {
        bool wantsHtml = WantsHtml(Request);

        if (string.IsNullOrWhiteSpace(client_id) || string.IsNullOrWhiteSpace(redirect_uri))
            return ErrorResponse("client_id e redirect_uri são obrigatórios.", wantsHtml);

        if (!string.Equals(response_type?.Trim(), "code", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(response_type))
            return ErrorResponse("response_type deve ser 'code' (Authorization Code).", wantsHtml);

        if (!string.IsNullOrWhiteSpace(code_challenge))
        {
            var method = code_challenge_method?.Trim();
            if (!string.IsNullOrEmpty(method) && method != "S256" && !string.Equals(method, "plain", StringComparison.OrdinalIgnoreCase))
                return ErrorResponse("code_challenge_method deve ser S256 ou plain.", wantsHtml);
        }

        var normalizedScope = NormalizeScope(scope);
        if (normalizedScope is null)
            return ErrorResponse("scope deve conter apenas: openid, profile, email (ex.: openid profile email).", wantsHtml);

        var client = await _oauthService.GetClientByIdAsync(client_id!, cancellationToken);
        if (client is null)
            return ErrorResponse("Cliente não encontrado. Cadastre o client_id no servidor (ex.: seed ou ferramenta administrativa).", wantsHtml);

        if (!_oauthService.IsRedirectUriAllowed(client, redirect_uri!))
            return ErrorResponse("redirect_uri não permitido para este cliente. O URI deve estar cadastrado em RedirectUris do cliente.", wantsHtml);

        var loginUrl = BuildLoginUiRedirectUrl(_loginUi.SignInPath, client_id, redirect_uri, state ?? "", normalizedScope, code_challenge, code_challenge_method, error: null);
        return Redirect(loginUrl);
    }

    private static string BuildLoginUiRedirectUrl(string signInPath, string clientId, string redirectUri, string state, string scope, string? codeChallenge, string? codeChallengeMethod, string? error)
    {
        var q = new List<string>
        {
            "client_id=" + Uri.EscapeDataString(clientId),
            "redirect_uri=" + Uri.EscapeDataString(redirectUri),
            "state=" + Uri.EscapeDataString(state),
            "scope=" + Uri.EscapeDataString(scope)
        };
        if (!string.IsNullOrWhiteSpace(codeChallenge))
        {
            q.Add("code_challenge=" + Uri.EscapeDataString(codeChallenge.Trim()));
            q.Add("code_challenge_method=" + Uri.EscapeDataString((codeChallengeMethod ?? "S256").Trim()));
        }
        if (!string.IsNullOrEmpty(error))
            q.Add("error=" + Uri.EscapeDataString(error));
        var sep = signInPath.Contains('?') ? "&" : "?";
        return signInPath + sep + string.Join("&", q);
    }

    private static bool WantsHtml(HttpRequest request)
    {
        var accept = request.Headers.Accept.FirstOrDefault();
        if (string.IsNullOrEmpty(accept)) return true;
        return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult ErrorResponse(string message, bool asHtml)
    {
        if (asHtml)
        {
            var html = GetErrorPageHtml(message);
            return new ContentResult
            {
                Content = html,
                ContentType = "text/html; charset=utf-8",
                StatusCode = 400
            };
        }
        return BadRequest(message);
    }

    private static string GetErrorPageHtml(string message)
    {
        var encoded = WebUtility.HtmlEncode(message);
        return $@"<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""/><meta name=""viewport"" content=""width=device-width,initial-scale=1""/><title>Erro - Autorização</title></head>
<body>
  <h2>Erro na autorização</h2>
  <p>{encoded}</p>
  <p><a href=""javascript:history.back()"">Voltar</a></p>
</body>
</html>";
    }

    private static readonly HashSet<string> AllowedScopes = new(StringComparer.OrdinalIgnoreCase) { "openid", "profile", "email" };

    private static string? NormalizeScope(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope)) return "openid";
        var parts = scope!.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var list = new List<string>();
        foreach (var p in parts)
        {
            if (!AllowedScopes.Contains(p)) return null;
            if (!list.Contains(p, StringComparer.OrdinalIgnoreCase)) list.Add(p);
        }
        return list.Count == 0 ? "openid" : string.Join(" ", list);
    }

    /// <summary>POST: processa login e redireciona para redirect_uri com ?code=...&state=...</summary>
    [HttpPost("authorize")]
    public async Task<IActionResult> AuthorizePost(
        [FromForm] string? username,
        [FromForm] string? password,
        [FromForm] string? client_id,
        [FromForm] string? redirect_uri,
        [FromForm] string? state,
        [FromForm] string? scope,
        [FromForm] string? code_challenge,
        [FromForm] string? code_challenge_method,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(client_id) || string.IsNullOrWhiteSpace(redirect_uri))
            return BadRequest("username, password, client_id e redirect_uri são obrigatórios.");

        var client = await _oauthService.GetClientByIdAsync(client_id, cancellationToken);
        if (client is null || !_oauthService.IsRedirectUriAllowed(client, redirect_uri))
            return BadRequest("Cliente ou redirect_uri inválido.");

        var normalizedScope = NormalizeScope(scope);
        if (normalizedScope is null)
            return BadRequest("scope inválido. Use apenas: openid, profile, email.");

        var normalizedUserName = username.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName.ToLower() == normalizedUserName, cancellationToken);
        if (user is null || !_passwordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
        {
            var loginUrl = BuildLoginUiRedirectUrl(_loginUi.SignInPath, client_id, redirect_uri, state ?? "", normalizedScope, code_challenge, code_challenge_method, error: "Usuário ou senha inválidos.");
            return Redirect(loginUrl);
        }

        var authCode = await _oauthService.CreateAuthorizationCodeAsync(user, client, redirect_uri, normalizedScope, state, code_challenge, code_challenge_method, cancellationToken);
        var redirectUrl = BuildRedirectUrl(redirect_uri, authCode.Code, state);
        return Redirect(redirectUrl);
    }

    /// <summary>POST: troca authorization code por tokens (grant_type=authorization_code) ou renova com refresh_token (grant_type=refresh_token).</summary>
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token(
        [FromForm] string? grant_type,
        [FromForm] string? code,
        [FromForm] string? redirect_uri,
        [FromForm] string? code_verifier,
        [FromForm] string? refresh_token,
        [FromForm] string? client_id,
        [FromForm] string? client_secret,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(client_id) || string.IsNullOrWhiteSpace(client_secret))
            return BadRequest(new { error = "invalid_request", error_description = "client_id e client_secret são obrigatórios." });

        var client = await _oauthService.ValidateClientAsync(client_id, client_secret, cancellationToken);
        if (client is null)
            return Unauthorized(new { error = "invalid_client", error_description = "client_id ou client_secret inválidos." });

        if (grant_type == "authorization_code")
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(redirect_uri))
                return BadRequest(new { error = "invalid_request", error_description = "code e redirect_uri são obrigatórios para grant_type=authorization_code." });

            var consumed = await _oauthService.ConsumeAuthorizationCodeAsync(code, redirect_uri, client_id, code_verifier, cancellationToken);
            if (consumed is null)
                return BadRequest(new
                {
                    error = "invalid_grant",
                    error_description = "Código inválido, expirado ou já utilizado. Se usou PKCE no /authorize (code_challenge), envie code_verifier no token; redirect_uri deve ser idêntico."
                });

            var (user, _, scope) = consumed.Value;
            var accessToken = _jwtService.CreateAccessToken(user, scope);
            var idToken = _jwtService.CreateIdToken(user, client_id, scope);
            var newRefreshToken = await _refreshTokenService.CreateAsync(user, cancellationToken);

            return Ok(new
            {
                access_token = accessToken,
                refresh_token = newRefreshToken.Token,
                id_token = idToken,
                token_type = "Bearer",
                expires_in = _jwtSettings.AccessTokenExpirationMinutes * 60
            });
        }

        if (grant_type == "refresh_token")
        {
            if (string.IsNullOrWhiteSpace(refresh_token))
                return BadRequest(new { error = "invalid_request", error_description = "refresh_token é obrigatório para grant_type=refresh_token." });

            var refreshTokenEntity = await _refreshTokenService.FindValidAsync(refresh_token.Trim(), cancellationToken);
            if (refreshTokenEntity is null)
                return BadRequest(new { error = "invalid_grant", error_description = "Refresh token inválido ou expirado." });

            await _refreshTokenService.RevokeAsync(refreshTokenEntity, cancellationToken);
            var accessToken = _jwtService.CreateAccessToken(refreshTokenEntity.User, scope: null);
            var idToken = _jwtService.CreateIdToken(refreshTokenEntity.User, client_id, scope: null);
            var newRefreshToken = await _refreshTokenService.CreateAsync(refreshTokenEntity.User, cancellationToken);

            return Ok(new
            {
                access_token = accessToken,
                refresh_token = newRefreshToken.Token,
                id_token = idToken,
                token_type = "Bearer",
                expires_in = _jwtSettings.AccessTokenExpirationMinutes * 60
            });
        }

        return BadRequest(new { error = "unsupported_grant_type", error_description = "Suportado: authorization_code, refresh_token." });
    }

    /// <summary>Revoga um refresh_token (RFC 7009). Sempre retorna 200; não revela se o token era válido.</summary>
    [HttpPost("revoke")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Revoke(
        [FromForm] string? token,
        [FromForm] string? token_type_hint,
        [FromForm] string? client_id,
        [FromForm] string? client_secret,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Ok();

        if (string.IsNullOrWhiteSpace(client_id) || string.IsNullOrWhiteSpace(client_secret))
            return Ok();

        var client = await _oauthService.ValidateClientAsync(client_id, client_secret, cancellationToken);
        if (client is null)
            return Ok();

        var hint = token_type_hint?.Trim();
        if (string.IsNullOrEmpty(hint) || string.Equals(hint, "refresh_token", StringComparison.OrdinalIgnoreCase))
        {
            var refreshTokenEntity = await _refreshTokenService.FindValidAsync(token.Trim(), cancellationToken);
            if (refreshTokenEntity is not null)
                await _refreshTokenService.RevokeAsync(refreshTokenEntity, cancellationToken);
        }

        return Ok();
    }

    /// <summary>OpenID Connect: retorna claims do usuário conforme scope do access_token (sub sempre; name/preferred_username com profile; email com email).</summary>
    [HttpGet("userinfo")]
    [Authorize]
    [Produces("application/json")]
    public IActionResult Userinfo()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(sub))
            return Unauthorized();

        var scopeClaim = User.FindFirst("scope")?.Value;
        var scopeSet = ParseScopeClaim(scopeClaim);

        var result = new Dictionary<string, object?> { ["sub"] = sub };
        if (scopeSet.Contains("profile") || scopeSet.Count == 0)
        {
            var name = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("unique_name")?.Value;
            result["name"] = name;
            result["preferred_username"] = name ?? User.FindFirst("preferred_username")?.Value;
        }
        if (scopeSet.Contains("email") || scopeSet.Count == 0)
            result["email"] = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;

        return Ok(result);
    }

    private static HashSet<string> ParseScopeClaim(string? scope)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(scope)) return set;
        foreach (var s in scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            set.Add(s);
        return set;
    }

    private static string BuildRedirectUrl(string redirectUri, string code, string? state)
    {
        var sep = redirectUri.Contains('?') ? "&" : "?";
        var url = $"{redirectUri}{sep}code={Uri.EscapeDataString(code)}";
        if (!string.IsNullOrEmpty(state))
            url += $"&state={Uri.EscapeDataString(state)}";
        return url;
    }
}
