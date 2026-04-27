using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using lrfdev.auth.api.Contracts;
using lrfdev.auth.api.Data;
using lrfdev.auth.api.Security;
using lrfdev.auth.api.Services;

namespace lrfdev.auth.api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AuthDbContext context,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IPermissionResolver permissionResolver,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized();
        }

        var normalizedUsername = request.Username.Trim();

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == normalizedUsername && x.IsActive);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized();
        }

        var permissions = await permissionResolver.ResolveAsync(user.Id);

        var token = tokenService.GenerateAccessToken(user, permissions);
        var expiresIn = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var minutes)
            ? minutes * 60
            : 3600;

        return Ok(new LoginResponse
        {
            AccessToken = token,
            ExpiresIn = expiresIn,
            Username = user.Username,
            Permissions = permissions
        });
    }
}
