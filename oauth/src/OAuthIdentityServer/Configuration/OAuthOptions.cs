namespace OAuthIdentityServer.Configuration;

public class OAuthOptions
{
    public const string SectionName = "OAuth";

    public string Issuer { get; set; } = "";
    public string SigningKey { get; set; } = "";
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
    public int RefreshTokenLifetimeDays { get; set; } = 30;
    public int AuthorizationCodeLifetimeMinutes { get; set; } = 10;
}
