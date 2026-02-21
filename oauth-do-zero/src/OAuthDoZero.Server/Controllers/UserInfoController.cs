using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuthDoZero.Server.DTOs;
using OAuthDoZero.Server.Services;
using System.Security.Claims;

namespace OAuthDoZero.Server.Controllers;

/// <summary>
/// Controller para endpoint UserInfo (OpenID Connect)
/// </summary>
[ApiController]
[Route("oauth")]
[Authorize] // Requer token válido
public class UserInfoController : ControllerBase
{
    private readonly IOAuthService _oauthService;

    public UserInfoController(IOAuthService oauthService)
    {
        _oauthService = oauthService;
    }

    /// <summary>
    /// Endpoint UserInfo do OpenID Connect
    /// </summary>
    [HttpGet("userinfo")]
    public async Task<IActionResult> UserInfo()
    {
        try
        {
            // Obter subject do token
            var subjectClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (subjectClaim == null)
                return BadRequest(new { error = "invalid_token", error_description = "No subject found in token" });

            // Buscar usuário
            var user = await _oauthService.GetUserByIdAsync(subjectClaim.Value);
            if (user == null || !user.IsActive)
                return BadRequest(new { error = "invalid_token", error_description = "User not found or inactive" });

            // Construir resposta baseada nos scopes
            var response = new UserInfoResponse
            {
                Subject = user.Id
            };

            // Verificar scopes do token para incluir claims apropriados
            var scopeClaim = User.FindFirst("scope");
            if (scopeClaim != null)
            {
                var scopes = scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Incluir claims baseado nos scopes
                if (scopes.Contains("profile"))
                {
                    response.GivenName = user.FirstName;
                    response.FamilyName = user.LastName;
                    response.PreferredUsername = user.PreferredUsername ?? user.Username;
                    response.Picture = user.Picture;
                    response.Locale = user.Locale;
                    response.TimeZone = user.TimeZone;

                    var fullName = $"{user.FirstName} {user.LastName}".Trim();
                    if (!string.IsNullOrEmpty(fullName))
                        response.Name = fullName;
                    else if (!string.IsNullOrEmpty(user.PreferredUsername))
                        response.Name = user.PreferredUsername;
                    else
                        response.Name = user.Username;
                }

                if (scopes.Contains("email"))
                {
                    response.Email = user.Email;
                    response.EmailVerified = user.EmailConfirmed;
                }

                // UpdatedAt sempre inclui se tiver dados de perfil
                if (scopes.Any(s => s is "profile" or "email"))
                {
                    response.UpdatedAt = user.LastLoginAt?.Subtract(DateTime.UnixEpoch).Ticks / TimeSpan.TicksPerSecond;
                }
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "server_error", error_description = "Internal server error" });
        }
    }
}