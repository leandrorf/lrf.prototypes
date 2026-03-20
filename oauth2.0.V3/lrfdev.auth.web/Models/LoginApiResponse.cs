namespace lrfdev.auth.web.Models;

public sealed class LoginApiResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public string Username { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Permissions { get; set; } = [];
}
