using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using lrfdev.auth.api.Contracts;
using lrfdev.auth.api.Data;
using lrfdev.auth.api.OAuth;
using lrfdev.auth.api.Security;
using lrfdev.auth.api.Services;
using System.Security.Cryptography;
using System.Text;

namespace lrfdev.auth.api.Controllers;

[ApiController]
[Route("connect")]
public sealed partial class ConnectController(
    AuthDbContext context,
    ITokenService tokenService,
    IPermissionResolver permissionResolver,
    IPasswordHasher passwordHasher,
    IConfiguration configuration) : ControllerBase
{
    private int AccessTokenLifetimeSeconds =>
        int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var minutes)
            ? minutes * 60
            : 3600;

    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize(
        [FromQuery(Name = "response_type")] string responseType,
        [FromQuery(Name = "client_id")] string clientId,
        [FromQuery(Name = "redirect_uri")] string redirectUri,
        [FromQuery(Name = "scope")] string? scope,
        [FromQuery(Name = "state")] string? state,
        [FromQuery(Name = "code_challenge")] string codeChallenge,
        [FromQuery(Name = "code_challenge_method")] string? codeChallengeMethod,
        [FromQuery(Name = "username")] string username)
    {
        if (!string.Equals(responseType, "code", StringComparison.Ordinal))
        {
            return BadRequest(new { error = "unsupported_response_type" });
        }

        var client = await context.OAuthClients.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientId == clientId && x.IsActive);
        if (client is null)
        {
            return BadRequest(new { error = "invalid_client" });
        }

        var redirectUris = ParseList(client.RedirectUris);
        if (!redirectUris.Contains(redirectUri, StringComparer.Ordinal))
        {
            return BadRequest(new { error = "invalid_request", error_description = "invalid redirect_uri" });
        }

        if (client.RequirePkce)
        {
            if (string.IsNullOrWhiteSpace(codeChallenge))
            {
                return BadRequest(new { error = "invalid_request", error_description = "code_challenge required" });
            }

            if (!string.Equals(codeChallengeMethod ?? "S256", "S256", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "invalid_request", error_description = "only S256 is supported" });
            }
        }

        var requestedScopes = ParseList(scope);
        var allowedScopes = ParseList(client.AllowedScopes);
        if (requestedScopes.Except(allowedScopes, StringComparer.Ordinal).Any())
        {
            return BadRequest(new { error = "invalid_scope" });
        }

        var user = await context.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username && x.IsActive);
        if (user is null)
        {
            return Unauthorized(new { error = "access_denied", error_description = "user not authenticated" });
        }

        var code = Base64Url(RandomNumberGenerator.GetBytes(32));
        context.OAuthAuthorizationCodes.Add(new Models.OAuthAuthorizationCode
        {
            Id = Guid.NewGuid(),
            Code = code,
            ClientId = client.Id,
            UserId = user.Id,
            RedirectUri = redirectUri,
            Scope = string.Join(' ', requestedScopes),
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod ?? "S256",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
        });
        await context.SaveChangesAsync();

        var location = $"{redirectUri}?code={Uri.EscapeDataString(code)}";
        if (!string.IsNullOrWhiteSpace(state))
        {
            location += $"&state={Uri.EscapeDataString(state)}";
        }

        return Redirect(location);
    }

    [HttpPost("token")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Token(
        [FromForm(Name = "grant_type")] string? grantType,
        [FromForm(Name = "client_id")] string? clientId,
        [FromForm(Name = "client_secret")] string? clientSecret,
        [FromForm(Name = "code")] string? code,
        [FromForm(Name = "redirect_uri")] string? redirectUri,
        [FromForm(Name = "code_verifier")] string? codeVerifier,
        [FromForm(Name = "scope")] string? scope,
        [FromForm(Name = "device_code")] string? deviceCode)
    {
        if (string.Equals(grantType, "authorization_code", StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(clientId) ||
                string.IsNullOrWhiteSpace(code) ||
                string.IsNullOrWhiteSpace(redirectUri) ||
                string.IsNullOrWhiteSpace(codeVerifier))
            {
                return BadRequest(new { error = "invalid_request" });
            }

            var authCode = await context.OAuthAuthorizationCodes
                .Include(x => x.Client)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Code == code);

            if (authCode is null || authCode.ConsumedAtUtc.HasValue || authCode.ExpiresAtUtc < DateTime.UtcNow)
            {
                return BadRequest(new { error = "invalid_grant" });
            }

            if (!string.Equals(authCode.Client.ClientId, clientId, StringComparison.Ordinal))
            {
                return BadRequest(new { error = "invalid_client" });
            }

            if (!string.Equals(authCode.RedirectUri, redirectUri, StringComparison.Ordinal))
            {
                return BadRequest(new { error = "invalid_grant" });
            }

            if (!ValidatePkce(codeVerifier, authCode.CodeChallenge))
            {
                return BadRequest(new { error = "invalid_grant", error_description = "invalid code_verifier" });
            }

            var permissions = await permissionResolver.ResolveAsync(authCode.UserId);
            var accessToken = tokenService.GenerateAccessToken(authCode.User, permissions);
            authCode.ConsumedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return Ok(new TokenResponse
            {
                AccessToken = accessToken,
                ExpiresIn = AccessTokenLifetimeSeconds,
                Scope = authCode.Scope
            });
        }

        if (string.Equals(grantType, "client_credentials", StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                return BadRequest(new { error = "invalid_request" });
            }

            var client = await context.OAuthClients.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ClientId == clientId && x.IsActive && x.IsConfidentialClient);
            if (client is null || string.IsNullOrWhiteSpace(client.ClientSecretHash))
            {
                return BadRequest(new { error = "invalid_client" });
            }

            if (!passwordHasher.Verify(clientSecret, client.ClientSecretHash))
            {
                return BadRequest(new { error = "invalid_client" });
            }

            var allowedScopes = ParseList(client.AllowedScopes);
            var requestedScopes = ParseList(scope);
            var grantedScopes = requestedScopes.Length == 0 ? allowedScopes : requestedScopes;
            if (grantedScopes.Except(allowedScopes, StringComparer.Ordinal).Any())
            {
                return BadRequest(new { error = "invalid_scope" });
            }

            var token = tokenService.GenerateClientAccessToken(client.ClientId, grantedScopes);
            return Ok(new TokenResponse
            {
                AccessToken = token,
                ExpiresIn = AccessTokenLifetimeSeconds,
                Scope = string.Join(' ', grantedScopes)
            });
        }

        if (string.Equals(grantType, OAuthConstants.GrantTypeDeviceCode, StringComparison.Ordinal))
        {
            return await ExchangeDeviceCodeAsync(clientId, clientSecret, deviceCode);
        }

        return BadRequest(new { error = "unsupported_grant_type" });
    }

    private static bool ValidatePkce(string codeVerifier, string codeChallenge)
    {
        if (string.IsNullOrWhiteSpace(codeVerifier))
        {
            return false;
        }

        var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        var computed = Base64Url(bytes);
        return string.Equals(computed, codeChallenge, StringComparison.Ordinal);
    }

    private static string Base64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string[] ParseList(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
