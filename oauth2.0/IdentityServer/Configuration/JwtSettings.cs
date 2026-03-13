namespace IdentityServer.Configuration;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>Chave secreta para assinar o token (mín. 32 caracteres em produção).</summary>
    public string Secret { get; set; } = default!;

    /// <summary>Emissor do token (ex: https://localhost:5001).</summary>
    public string Issuer { get; set; } = default!;

    /// <summary>Audiência do token (ex: identity-server ou o client_id).</summary>
    public string Audience { get; set; } = default!;

    /// <summary>Tempo de vida do access token em minutos.</summary>
    public int AccessTokenExpirationMinutes { get; set; } = 60;

    /// <summary>Tempo de vida do refresh token em dias.</summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
