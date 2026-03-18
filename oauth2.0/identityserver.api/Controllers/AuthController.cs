using identityserver.api.Configuration;
using identityserver.api.Data;
using identityserver.api.Models;
using identityserver.api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace identityserver.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string DefaultClientId = "meu-app";

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

    /// <summary>Login direto: retorna access_token, refresh_token e id_token (usado pela página de login do identityserver.web).</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedUserName = request.UserName.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName.ToLower() == normalizedUserName, cancellationToken);

        if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            return BadRequest(new { error = "INVALID_CREDENTIALS" });

        var scope = "openid profile email";
        var accessToken = _jwtService.CreateAccessToken(user, scope);
        var idToken = _jwtService.CreateIdToken(user, DefaultClientId, scope);
        var refreshToken = await _refreshTokenService.CreateAsync(user, cancellationToken);

        return Ok(new
        {
            access_token = accessToken,
            refresh_token = refreshToken.Token,
            id_token = idToken,
            token_type = "Bearer",
            expires_in = _jwtSettings.AccessTokenExpirationMinutes * 60
        });
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
            return BadRequest(new { error = "USERNAME_ALREADY_EXISTS" });

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
}
