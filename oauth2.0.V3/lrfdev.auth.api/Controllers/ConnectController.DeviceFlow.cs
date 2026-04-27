using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using lrfdev.auth.api.Contracts;
using lrfdev.auth.api.Models;
using lrfdev.auth.api.OAuth;
using System.Security.Cryptography;

namespace lrfdev.auth.api.Controllers;

public sealed partial class ConnectController
{
    private const int DeviceFlowExpiresSeconds = 900;
    private const int DevicePollIntervalSeconds = 5;

    /// <summary>RFC 8628 — inicia o fluxo (TV/IoT obtém códigos e URIs).</summary>
    [HttpPost("deviceauthorization")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType<DeviceAuthorizationResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeviceAuthorization(
        [FromForm(Name = "client_id")] string? clientId,
        [FromForm(Name = "client_secret")] string? clientSecret,
        [FromForm(Name = "scope")] string? scope)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return BadRequest(new { error = "invalid_request", error_description = "client_id is required" });
        }

        var client = await context.OAuthClients.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientId == clientId && x.IsActive);
        if (client is null || !client.AllowDeviceAuthorization)
        {
            return BadRequest(new { error = "unauthorized_client" });
        }

        if (client.IsConfidentialClient)
        {
            if (string.IsNullOrWhiteSpace(clientSecret) ||
                string.IsNullOrWhiteSpace(client.ClientSecretHash) ||
                !passwordHasher.Verify(clientSecret, client.ClientSecretHash))
            {
                return BadRequest(new { error = "invalid_client" });
            }
        }

        var allowedScopes = ParseList(client.AllowedScopes);
        var requestedScopes = ParseList(scope);
        var grantedScopes = requestedScopes.Length == 0 ? allowedScopes : requestedScopes;
        if (grantedScopes.Except(allowedScopes, StringComparer.Ordinal).Any())
        {
            return BadRequest(new { error = "invalid_scope" });
        }

        var verificationBase = configuration["DeviceAuthorization:VerificationBaseUrl"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("DeviceAuthorization:VerificationBaseUrl is not configured.");

        string userCode;
        string deviceCode = Base64Url(RandomNumberGenerator.GetBytes(48));
        const int maxAttempts = 8;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            userCode = GenerateUserCode();
            var exists = await context.OAuthDeviceFlowSessions.AnyAsync(x => x.UserCode == userCode);
            if (exists)
            {
                continue;
            }

            var session = new OAuthDeviceFlowSession
            {
                Id = Guid.NewGuid(),
                DeviceCode = deviceCode,
                UserCode = userCode,
                ClientId = client.Id,
                Scope = string.Join(' ', grantedScopes),
                Status = "pending",
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddSeconds(DeviceFlowExpiresSeconds),
                IntervalSeconds = DevicePollIntervalSeconds
            };
            context.OAuthDeviceFlowSessions.Add(session);
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                context.Entry(session).State = EntityState.Detached;
                continue;
            }

            var verificationUri = $"{verificationBase}";
            var verificationComplete =
                $"{verificationBase}?user_code={Uri.EscapeDataString(userCode)}";

            return Ok(new DeviceAuthorizationResponse
            {
                DeviceCode = deviceCode,
                UserCode = userCode,
                VerificationUri = verificationUri,
                VerificationUriComplete = verificationComplete,
                ExpiresIn = DeviceFlowExpiresSeconds,
                Interval = DevicePollIntervalSeconds
            });
        }

        return StatusCode(StatusCodes.Status500InternalServerError,
            new { error = "server_error", error_description = "could not allocate user_code" });
    }

    /// <summary>Usuário confirma no celular (após escanear QR ou abrir o link).</summary>
    [HttpPost("device")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> CompleteDeviceUserAuthorization(
        [FromForm(Name = "user_code")] string? userCode,
        [FromForm(Name = "username")] string? username,
        [FromForm(Name = "password")] string? password)
    {
        if (string.IsNullOrWhiteSpace(userCode) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            return BadRequest(new { error = "invalid_request" });
        }

        var normalizedUserCode = NormalizeUserCode(userCode);
        var session = await context.OAuthDeviceFlowSessions
            .FirstOrDefaultAsync(x => x.UserCode == normalizedUserCode);

        if (session is null)
        {
            return BadRequest(new { error = "invalid_grant", error_description = "invalid user_code" });
        }

        if (session.ExpiresAtUtc < DateTime.UtcNow)
        {
            session.Status = "denied";
            await context.SaveChangesAsync();
            return BadRequest(new { error = "expired_token" });
        }

        if (session.Status != "pending")
        {
            return BadRequest(new { error = "invalid_grant", error_description = "flow is no longer pending" });
        }

        var user = await context.Users.FirstOrDefaultAsync(x => x.Username == username.Trim() && x.IsActive);
        if (user is null || !passwordHasher.Verify(password, user.PasswordHash))
        {
            return Unauthorized(new { error = "access_denied", error_description = "invalid credentials" });
        }

        session.UserId = user.Id;
        session.Status = "approved";
        await context.SaveChangesAsync();

        return Ok(new { status = "approved", message = "Dispositivo autorizado. Você pode fechar esta página." });
    }

    private async Task<IActionResult> ExchangeDeviceCodeAsync(
        string? clientId,
        string? clientSecret,
        string? deviceCode)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(deviceCode))
        {
            return BadRequest(new { error = "invalid_request" });
        }

        var session = await context.OAuthDeviceFlowSessions
            .Include(x => x.Client)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.DeviceCode == deviceCode);

        if (session is null)
        {
            return BadRequest(new { error = "invalid_grant" });
        }

        if (!string.Equals(session.Client.ClientId, clientId, StringComparison.Ordinal))
        {
            return BadRequest(new { error = "invalid_client" });
        }

        if (session.Client.IsConfidentialClient)
        {
            if (string.IsNullOrWhiteSpace(clientSecret) ||
                string.IsNullOrWhiteSpace(session.Client.ClientSecretHash) ||
                !passwordHasher.Verify(clientSecret, session.Client.ClientSecretHash))
            {
                return BadRequest(new { error = "invalid_client" });
            }
        }

        var now = DateTime.UtcNow;
        if (session.ExpiresAtUtc < now)
        {
            if (session.Status != "consumed")
            {
                session.Status = "denied";
                await context.SaveChangesAsync();
            }

            return BadRequest(new { error = "expired_token" });
        }

        if (session.Status == "consumed")
        {
            return BadRequest(new { error = "invalid_grant" });
        }

        if (session.Status == "denied")
        {
            return BadRequest(new { error = "access_denied" });
        }

        if (session.LastPollAtUtc.HasValue &&
            now < session.LastPollAtUtc.Value.AddSeconds(session.IntervalSeconds))
        {
            return BadRequest(new { error = "slow_down" });
        }

        session.LastPollAtUtc = now;

        if (session.Status == "pending")
        {
            await context.SaveChangesAsync();
            return BadRequest(new { error = "authorization_pending" });
        }

        if (session.Status == "approved" && session.User is not null && session.UserId.HasValue)
        {
            var permissions = await permissionResolver.ResolveAsync(session.UserId.Value);
            var accessToken = tokenService.GenerateAccessToken(session.User, permissions);
            session.Status = "consumed";
            await context.SaveChangesAsync();

            return Ok(new TokenResponse
            {
                AccessToken = accessToken,
                ExpiresIn = AccessTokenLifetimeSeconds,
                Scope = session.Scope
            });
        }

        await context.SaveChangesAsync();
        return BadRequest(new { error = "invalid_grant" });
    }

    private static string GenerateUserCode()
    {
        const string alphabet = "BCDFGHJKLMNPQRSTVWXZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(8);
        var chars = new char[8];
        for (var i = 0; i < 8; i++)
        {
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        }

        return $"{new string(chars, 0, 4)}-{new string(chars, 4, 4)}";
    }

    private static string NormalizeUserCode(string userCode)
    {
        var alnum = new string(userCode.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        return alnum.Length == 8
            ? $"{alnum[..4]}-{alnum[4..]}"
            : userCode.Trim().ToUpperInvariant();
    }
}
