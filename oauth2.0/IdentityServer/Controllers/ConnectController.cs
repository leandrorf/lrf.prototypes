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

/// <summary>Endpoints OAuth 2.0: authorize (login + code), token e registro de cliente.</summary>
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

    /// <summary>GET: exibe formulário de login OAuth. Parâmetros: client_id, redirect_uri, response_type=code, state, scope (opcional).</summary>
    [HttpGet("authorize")]
    [Produces("text/html")]
    public async Task<IActionResult> Authorize(
        [FromQuery] string client_id,
        [FromQuery] string redirect_uri,
        [FromQuery] string? response_type,
        [FromQuery] string? state,
        [FromQuery] string? scope,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(client_id) || string.IsNullOrWhiteSpace(redirect_uri))
            return BadRequest("client_id e redirect_uri são obrigatórios.");

        if (response_type != "code")
            return BadRequest("response_type deve ser 'code' (Authorization Code).");

        var client = await _oauthService.GetClientByIdAsync(client_id, cancellationToken);
        if (client is null)
            return BadRequest("Cliente não encontrado.");

        if (!_oauthService.IsRedirectUriAllowed(client, redirect_uri))
            return BadRequest("redirect_uri não permitido para este cliente.");

        var html = GetLoginPageHtml(client_id, redirect_uri, state ?? "", scope ?? "", Request);
        return Content(html, "text/html", Encoding.UTF8);
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
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(client_id) || string.IsNullOrWhiteSpace(redirect_uri))
            return BadRequest("username, password, client_id e redirect_uri são obrigatórios.");

        var client = await _oauthService.GetClientByIdAsync(client_id, cancellationToken);
        if (client is null || !_oauthService.IsRedirectUriAllowed(client, redirect_uri))
            return BadRequest("Cliente ou redirect_uri inválido.");

        var normalizedUserName = username.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName.ToLower() == normalizedUserName, cancellationToken);
        if (user is null || !_passwordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
        {
            var html = GetLoginPageHtml(client_id, redirect_uri, state ?? "", scope ?? "", Request, error: "Usuário ou senha inválidos.");
            return Content(html, "text/html", Encoding.UTF8);
        }

        var authCode = await _oauthService.CreateAuthorizationCodeAsync(user, client, redirect_uri, scope, state, cancellationToken);
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

            var consumed = await _oauthService.ConsumeAuthorizationCodeAsync(code, redirect_uri, client_id, cancellationToken);
            if (consumed is null)
                return BadRequest(new
                {
                    error = "invalid_grant",
                    error_description = "Código inválido, expirado ou já utilizado. Verifique: (1) code usado apenas uma vez, (2) redirect_uri idêntico ao do /authorize, (3) code com menos de 10 min."
                });

            var (user, _) = consumed.Value;
            var accessToken = _jwtService.CreateAccessToken(user);
            var idToken = _jwtService.CreateIdToken(user, client_id);
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
            var accessToken = _jwtService.CreateAccessToken(refreshTokenEntity.User);
            var idToken = _jwtService.CreateIdToken(refreshTokenEntity.User, client_id);
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

    /// <summary>OpenID Connect: retorna claims do usuário autenticado (Bearer access_token).</summary>
    [HttpGet("userinfo")]
    [Authorize]
    [Produces("application/json")]
    public IActionResult Userinfo()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        var name = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("unique_name")?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
        var preferredUsername = User.FindFirst("preferred_username")?.Value ?? name;

        if (string.IsNullOrEmpty(sub))
            return Unauthorized();

        return Ok(new
        {
            sub,
            name,
            email = email ?? (string?)null,
            preferred_username = preferredUsername
        });
    }

    /// <summary>Registra um cliente OAuth (útil em desenvolvimento). Em produção use fluxo administrativo.</summary>
    [HttpPost("register-client")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterClient([FromBody] RegisterClientRequest request, CancellationToken cancellationToken)
    {
        var clientId = request.ClientId.Trim();
        var existing = await _db.Clients.AsNoTracking().AnyAsync(c => c.ClientId == clientId, cancellationToken);
        if (existing)
            return BadRequest(new { error = "CLIENT_ID_ALREADY_EXISTS" });

        var (hash, salt) = _passwordHasher.HashPassword(request.ClientSecret);

        var client = new Client
        {
            ClientId = clientId,
            ClientSecretHash = hash,
            ClientSecretSalt = salt,
            Name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim(),
            RedirectUris = request.RedirectUris.Trim(),
            AllowedGrantTypes = "authorization_code,refresh_token"
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync(cancellationToken);

        return Created($"connect/authorize?client_id={Uri.EscapeDataString(client.ClientId)}", new
        {
            client_id = client.ClientId,
            name = client.Name,
            redirect_uris = client.RedirectUris
        });
    }

    private static string BuildRedirectUrl(string redirectUri, string code, string? state)
    {
        var sep = redirectUri.Contains('?') ? "&" : "?";
        var url = $"{redirectUri}{sep}code={Uri.EscapeDataString(code)}";
        if (!string.IsNullOrEmpty(state))
            url += $"&state={Uri.EscapeDataString(state)}";
        return url;
    }

    /// <summary>Página de callback para testes: exibe code e state após o login (use como redirect_uri ao testar).</summary>
    [HttpGet("callback-demo")]
    [Produces("text/html")]
    public IActionResult CallbackDemo([FromQuery] string? code, [FromQuery] string? state)
    {
        var codeEnc = WebUtility.HtmlEncode(code ?? "");
        var stateEnc = WebUtility.HtmlEncode(state ?? "");
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}".TrimEnd('/');
        var html = $@"<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""/><title>OAuth Callback (demo)</title></head>
<body>
  <h2>Autorização concluída</h2>
  <p>Use o <strong>code</strong> abaixo no POST /connect/token (grant_type=authorization_code).</p>
  <p><strong>Code:</strong> <code style=""background:#eee;padding:4px;word-break:break-all;"">{codeEnc}</code></p>
  <p><strong>State:</strong> <code style=""background:#eee;padding:4px;"">{stateEnc}</code></p>
  <p><small>redirect_uri para usar no token: <code>{WebUtility.HtmlEncode(baseUrl)}/connect/callback-demo</code></small></p>
</body>
</html>";
        return Content(html, "text/html", Encoding.UTF8);
    }

    private static string GetLoginPageHtml(string clientId, string redirectUri, string state, string scope, HttpRequest request, string? error = null)
    {
        var basePath = (request.PathBase.Value ?? "").TrimEnd('/');
        var actionUrl = $"{request.Scheme}://{request.Host}{basePath}/connect/authorize";
        var errorBlock = string.IsNullOrEmpty(error) ? "" : $@"<p style=""color: red;"">{WebUtility.HtmlEncode(error)}</p>";
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
    <div><label>Usuário:</label><input type=""text"" name=""username"" required /></div>
    <div><label>Senha:</label><input type=""password"" name=""password"" required /></div>
    <button type=""submit"">Entrar</button>
  </form>
</body>
</html>";
    }
}
