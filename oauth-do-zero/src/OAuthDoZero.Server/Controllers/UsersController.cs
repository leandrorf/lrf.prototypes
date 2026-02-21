using Microsoft.AspNetCore.Mvc;
using OAuthDoZero.Server.Data;
using OAuthDoZero.Server.Models;
using OAuthDoZero.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace OAuthDoZero.Server.Controllers;

/// <summary>
/// Controller para gerenciamento de usuários (apenas para desenvolvimento/teste)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly OAuthDbContext _context;
    private readonly ICryptographyService _cryptoService;

    public UsersController(OAuthDbContext context, ICryptographyService cryptoService)
    {
        _context = context;
        _cryptoService = cryptoService;
    }

    /// <summary>
    /// Criar usuário de teste (apenas para desenvolvimento)
    /// </summary>
    [HttpPost("create-test-user")]
    public async Task<IActionResult> CreateTestUser([FromBody] CreateUserRequest request)
    {
        try
        {
            // Verificar se usuário já existe
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);

            if (existingUser != null)
                return BadRequest(new { error = "User already exists" });

            // Crear usuário
            var salt = _cryptoService.GenerateSalt();
            var passwordHash = _cryptoService.HashPassword(request.Password, salt);

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                Salt = salt,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PreferredUsername = request.PreferredUsername ?? request.Username,
                IsActive = true,
                EmailConfirmed = true, // Para testes
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User created successfully",
                userId = user.Id,
                username = user.Username,
                email = user.Email
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Listar usuários (apenas para desenvolvimento)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var users = await _context.Users
                .Where(u => u.IsActive)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.PreferredUsername,
                    u.CreatedAt,
                    u.EmailConfirmed
                })
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }
}

/// <summary>
/// Request para criação de usuário
/// </summary>
public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PreferredUsername { get; set; }
}