namespace identityserver.web.Models;

public class LoginTokenViewModel
{
    public string? UserName { get; set; }
    public string? Error { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? IdToken { get; set; }
    public string? TokenType { get; set; }
    public int? ExpiresIn { get; set; }
    public bool Success => !string.IsNullOrEmpty(AccessToken);
}
