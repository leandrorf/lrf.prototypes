using Microsoft.AspNetCore.Mvc;
using OAuthIdentityServer.Services;

namespace OAuthIdentityServer.Controllers;

[Route("oauth")]
[ApiController]
public class OAuthController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;

    public OAuthController(IClientService clientService, IUserService userService, ITokenService tokenService)
    {
        _clientService = clientService;
        _userService = userService;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Endpoint de autorização OAuth 2.0. GET exibe formulário de login (ou redireciona).
    /// Parâmetros: response_type=code, client_id, redirect_uri, scope (opcional), state (opcional), code_challenge (PKCE), code_challenge_method (opcional), nonce (opcional OIDC).
    /// </summary>
    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize(
        [FromQuery] string response_type,
        [FromQuery] string client_id,
        [FromQuery] string redirect_uri,
        [FromQuery] string? scope = null,
        [FromQuery] string? state = null,
        [FromQuery] string? code_challenge = null,
        [FromQuery] string? code_challenge_method = null,
        [FromQuery] string? nonce = null,
        CancellationToken ct = default)
    {
        if (response_type != "code")
            return BadRequest(new { error = "unsupported_response_type", error_description = "Apenas response_type=code é suportado." });

        var client = await _clientService.GetByClientIdAsync(client_id, ct);
        if (client == null)
            return BadRequest(new { error = "invalid_client", error_description = "Cliente não encontrado." });

        if (!_clientService.ValidateRedirectUri(client, redirect_uri))
            return BadRequest(new { error = "invalid_request", error_description = "redirect_uri não permitido." });

        if (!_clientService.IsValidScope(client, scope))
            return BadRequest(new { error = "invalid_scope", error_description = "Escopo inválido." });

        if (client.RequirePkce && string.IsNullOrEmpty(code_challenge))
            return BadRequest(new { error = "invalid_request", error_description = "code_challenge é obrigatório (PKCE)." });

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Content($"""
            <!DOCTYPE html>
            <html><head><meta charset="utf-8"><title>Login</title></head>
            <body>
            <h1>Login</h1>
            <form method="post" action="{baseUrl}/oauth/authorize">
            <input type="hidden" name="response_type" value="{System.Net.WebUtility.HtmlEncode(response_type)}" />
            <input type="hidden" name="client_id" value="{System.Net.WebUtility.HtmlEncode(client_id)}" />
            <input type="hidden" name="redirect_uri" value="{System.Net.WebUtility.HtmlEncode(redirect_uri)}" />
            <input type="hidden" name="scope" value="{System.Net.WebUtility.HtmlEncode(scope ?? "")}" />
            <input type="hidden" name="state" value="{System.Net.WebUtility.HtmlEncode(state ?? "")}" />
            <input type="hidden" name="code_challenge" value="{System.Net.WebUtility.HtmlEncode(code_challenge ?? "")}" />
            <input type="hidden" name="code_challenge_method" value="{System.Net.WebUtility.HtmlEncode(code_challenge_method ?? "S256")}" />
            <input type="hidden" name="nonce" value="{System.Net.WebUtility.HtmlEncode(nonce ?? "")}" />
            <p><label>Usuário: <input type="text" name="username" required /></label></p>
            <p><label>Senha: <input type="password" name="password" required /></label></p>
            <p><button type="submit">Entrar</button></p>
            </form>
            </body></html>
            """, "text/html");
    }

    /// <summary>
    /// Processa o login e redireciona para redirect_uri com o authorization code.
    /// </summary>
    [HttpPost("authorize")]
    public async Task<IActionResult> AuthorizePost(
        [FromForm] string response_type,
        [FromForm] string client_id,
        [FromForm] string redirect_uri,
        [FromForm] string? scope,
        [FromForm] string? state,
        [FromForm] string? code_challenge,
        [FromForm] string? code_challenge_method,
        [FromForm] string? nonce,
        [FromForm] string username,
        [FromForm] string password,
        CancellationToken ct = default)
    {
        var client = await _clientService.GetByClientIdAsync(client_id, ct);
        if (client == null)
            return BadRequest(new { error = "invalid_client" });

        if (!_clientService.ValidateRedirectUri(client, redirect_uri))
            return BadRequest(new { error = "invalid_request" });

        var valid = await _userService.ValidateCredentialsAsync(username, password, ct);
        if (!valid)
            return Unauthorized(new { error = "access_denied", error_description = "Credenciais inválidas." });

        var user = await _userService.FindByUserNameAsync(username, ct);
        if (user == null)
            return Unauthorized(new { error = "access_denied" });

        var code = _tokenService.GenerateAuthorizationCode(client_id, user.Subject, redirect_uri, scope, code_challenge, code_challenge_method ?? "S256", nonce);
        var redirectUrl = $"{redirect_uri}?code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(state ?? "")}";
        return Redirect(redirectUrl);
    }

    /// <summary>
    /// Endpoint de tokens OAuth 2.0. Suporta grant_type: authorization_code e refresh_token.
    /// </summary>
    [HttpPost("token")]
    public async Task<IActionResult> Token(
        [FromForm] string grant_type,
        [FromForm] string? code,
        [FromForm] string? redirect_uri,
        [FromForm] string? client_id,
        [FromForm] string? client_secret,
        [FromForm] string? refresh_token,
        [FromForm] string? code_verifier,
        CancellationToken ct = default)
    {
        var clientId = client_id;
        var clientSecret = client_secret;
        if (string.IsNullOrEmpty(clientSecret))
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var decoded = Convert.FromBase64String(authHeader["Basic ".Length..].Trim());
                    var pair = System.Text.Encoding.UTF8.GetString(decoded).Split(':', 2);
                    if (pair.Length == 2)
                    {
                        clientId ??= pair[0];
                        clientSecret = pair[1];
                    }
                }
                catch { /* ignore */ }
            }
        }
        if (string.IsNullOrEmpty(clientId))
            return BadRequest(new { error = "invalid_client", error_description = "client_id é obrigatório." });

        var client = await _clientService.GetByClientIdAsync(clientId, ct);
        if (client == null)
            return BadRequest(new { error = "invalid_client" });

        if (!_clientService.ValidateClientSecret(client, clientSecret))
            return Unauthorized(new { error = "invalid_client", error_description = "Secret inválido." });

        if (!_clientService.IsValidGrantType(client, grant_type))
            return BadRequest(new { error = "unsupported_grant_type" });

        if (grant_type == "authorization_code")
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(redirect_uri))
                return BadRequest(new { error = "invalid_request", error_description = "code e redirect_uri são obrigatórios." });

            var result = await _tokenService.ExchangeCodeForTokensAsync(code!, redirect_uri!, code_verifier, clientId!, clientSecret);
            if (result == null)
                return BadRequest(new { error = "invalid_grant", error_description = "Código inválido ou expirado." });

            return Ok(new
            {
                access_token = result.AccessToken,
                token_type = "Bearer",
                expires_in = result.ExpiresIn,
                refresh_token = result.RefreshToken,
                scope = result.Scope,
                id_token = result.IdToken
            });
        }

        if (grant_type == "refresh_token")
        {
            if (string.IsNullOrEmpty(refresh_token))
                return BadRequest(new { error = "invalid_request", error_description = "refresh_token é obrigatório." });

            var result = await _tokenService.RefreshAccessTokenAsync(refresh_token!, clientId!, clientSecret);
            if (result == null)
                return BadRequest(new { error = "invalid_grant", error_description = "Refresh token inválido ou expirado." });

            return Ok(new
            {
                access_token = result.Value.AccessToken,
                token_type = "Bearer",
                expires_in = result.Value.ExpiresIn
            });
        }

        return BadRequest(new { error = "unsupported_grant_type" });
    }

    /// <summary>
    /// OIDC UserInfo - retorna claims do usuário autenticado via Bearer token.
    /// </summary>
    [HttpGet("userinfo")]
    [HttpPost("userinfo")]
    public async Task<IActionResult> UserInfo(CancellationToken ct = default)
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new { error = "invalid_token", error_description = "Token ausente ou inválido." });

        var token = authHeader["Bearer ".Length..].Trim();
        var user = await _tokenService.GetUserFromAccessTokenAsync(token);
        if (user == null)
            return Unauthorized(new { error = "invalid_token" });

        var claims = new Dictionary<string, object?>
        {
            ["sub"] = user.Subject,
            ["name"] = user.Name,
            ["given_name"] = user.GivenName,
            ["family_name"] = user.FamilyName,
            ["email"] = user.Email,
            ["email_verified"] = user.EmailConfirmed,
            ["preferred_username"] = user.UserName
        };
        return Ok(claims);
    }
}
