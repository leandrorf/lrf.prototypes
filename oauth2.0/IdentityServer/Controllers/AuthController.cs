using IdentityServer.Configuration;
using IdentityServer.Data;
using IdentityServer.Models;
using IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IdentityServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly JwtSettings _jwtSettings;

    public AuthController(
        AuthDbContext db,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        IOptions<JwtSettings> jwtSettings)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedUserName = request.UserName.Trim().ToLowerInvariant();

        var existing = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName.ToLower() == normalizedUserName, cancellationToken);

        if (existing is not null)
        {
            return BadRequest(new { error = "USERNAME_ALREADY_EXISTS" });
        }

        var (hash, salt) = _passwordHasher.HashPassword(request.Password);

        var user = new User
        {
            UserName = request.UserName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return Created($"/api/users/{user.Id}", new { user.Id, user.UserName, user.Email });
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedUserName = request.UserName.Trim().ToLowerInvariant();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserName.ToLower() == normalizedUserName, cancellationToken);

        if (user is null)
        {
            return BadRequest(new { error = "INVALID_CREDENTIALS" });
        }

        var valid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt);

        if (!valid)
        {
            return BadRequest(new { error = "INVALID_CREDENTIALS" });
        }

        var accessToken = _jwtService.CreateAccessToken(user);
        var refreshToken = await _refreshTokenService.CreateAsync(user, cancellationToken);

        return Ok(new
        {
            access_token = accessToken,
            refresh_token = refreshToken.Token,
            token_type = "Bearer",
            expires_in = _jwtSettings.AccessTokenExpirationMinutes * 60,
            refresh_token_expires_in = _jwtSettings.RefreshTokenExpirationDays * 24 * 60 * 60
        });
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { error = "REFRESH_TOKEN_REQUIRED" });
        }

        var refreshTokenEntity = await _refreshTokenService.FindValidAsync(request.RefreshToken.Trim(), cancellationToken);

        if (refreshTokenEntity is null)
        {
            return BadRequest(new { error = "INVALID_OR_EXPIRED_REFRESH_TOKEN" });
        }

        await _refreshTokenService.RevokeAsync(refreshTokenEntity, cancellationToken);
        var newAccessToken = _jwtService.CreateAccessToken(refreshTokenEntity.User);
        var newRefreshToken = await _refreshTokenService.CreateAsync(refreshTokenEntity.User, cancellationToken);

        return Ok(new
        {
            access_token = newAccessToken,
            refresh_token = newRefreshToken.Token,
            token_type = "Bearer",
            expires_in = _jwtSettings.AccessTokenExpirationMinutes * 60,
            refresh_token_expires_in = _jwtSettings.RefreshTokenExpirationDays * 24 * 60 * 60
        });
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                   ?? User.FindFirst("unique_name")?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                    ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(sub))
            return Unauthorized();

        return Ok(new
        {
            id = int.TryParse(sub, out var id) ? id : (int?)null,
            userName = name,
            email = email ?? (string?)null
        });
    }
}
