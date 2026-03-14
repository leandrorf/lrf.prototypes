using IdentityServer.Configuration;
using IdentityServer.Data;
using IdentityServer.Models;
using IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace IdentityServer.Controllers;

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

    public ConnectController(
        AuthDbContext db,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        IOAuthService oauthService,
        IOptions<JwtSettings> jwtSettings)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _oauthService = oauthService;
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>GET: exibe formulário de login OAuth. Parâmetros: client_id, redirect_uri, response_type=code, state, scope (opcional), code_challenge e code_challenge_method (PKCE).</summary>
    [HttpGet("authorize")]
    [Produces("text/html")]
    public async Task<IActionResult> Authorize(
        [FromQuery] string client_id,
        [FromQuery] string redirect_uri,
        [FromQuery] string? response_type,
        [FromQuery] string? state,
        [FromQuery] string? scope,
        [FromQuery] string? code_challenge,
        [FromQuery] string? code_challenge_method,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(client_id) || string.IsNullOrWhiteSpace(redirect_uri))
            return BadRequest("client_id e redirect_uri são obrigatórios.");

        if (response_type != "code")
            return BadRequest("response_type deve ser 'code' (Authorization Code).");

        if (!string.IsNullOrWhiteSpace(code_challenge))
        {
            var method = code_challenge_method?.Trim();
            if (!string.IsNullOrEmpty(method) && method != "S256" && !string.Equals(method, "plain", StringComparison.OrdinalIgnoreCase))
                return BadRequest("code_challenge_method deve ser S256 ou plain.");
        }

        var normalizedScope = NormalizeScope(scope);
        if (normalizedScope is null)
            return BadRequest("scope deve conter apenas: openid, profile, email (ex.: openid profile email).");

        var client = await _oauthService.GetClientByIdAsync(client_id, cancellationToken);
        if (client is null)
            return BadRequest("Cliente não encontrado.");

        if (!_oauthService.IsRedirectUriAllowed(client, redirect_uri))
            return BadRequest("redirect_uri não permitido para este cliente.");

        var html = GetLoginPageHtml(client_id, redirect_uri, state ?? "", normalizedScope, Request, codeChallenge: code_challenge, codeChallengeMethod: code_challenge_method);
        return Content(html, "text/html", Encoding.UTF8);
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
            var html = GetLoginPageHtml(client_id, redirect_uri, state ?? "", normalizedScope, Request, codeChallenge: code_challenge, codeChallengeMethod: code_challenge_method, error: "Usuário ou senha inválidos.");
            return Content(html, "text/html", Encoding.UTF8);
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

    private static string GetLoginPageHtml(string clientId, string redirectUri, string state, string scope, HttpRequest request, string? codeChallenge = null, string? codeChallengeMethod = null, string? error = null)
    {
        var basePath = (request.PathBase.Value ?? "").TrimEnd('/');
        var actionUrl = $"{request.Scheme}://{request.Host}{basePath}/connect/authorize";
        var errorBlock = string.IsNullOrEmpty(error) ? "" : $@"<p style=""color: red;"">{WebUtility.HtmlEncode(error)}</p>";
        var hiddenPkce = "";
        if (!string.IsNullOrWhiteSpace(codeChallenge))
        {
            hiddenPkce = $@"<input type=""hidden"" name=""code_challenge"" value=""{WebUtility.HtmlEncode(codeChallenge.Trim())}"" />
    <input type=""hidden"" name=""code_challenge_method"" value=""{WebUtility.HtmlEncode((codeChallengeMethod ?? "S256").Trim())}"" />";
        }
        return $@"<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""/><title>Login</title></head>
<body>
  <h2>Login</h2>
  {errorBlock}
  <form method=""post"" action=""{WebUtility.HtmlEncode(actionUrl)}"">
    <input type=""hidden"" name=""client_id"" value=""{WebUtility.HtmlEncode(clientId)}"" />
    <input type=""hidden"" name=""redirect_uri"" value=""{WebUtility.HtmlEncode(redirectUri)}"" />
    <input type=""hidden"" name=""state"" value=""{WebUtility.HtmlEncode(state)}"" />
    <input type=""hidden"" name=""scope"" value=""{WebUtility.HtmlEncode(scope)}"" />
    {hiddenPkce}
    <div><label>Usuário:</label><input type=""text"" name=""username"" required /></div>
    <div><label>Senha:</label><input type=""password"" name=""password"" required /></div>
    <button type=""submit"">Entrar</button>
  </form>
</body>
</html>";
    }
}
