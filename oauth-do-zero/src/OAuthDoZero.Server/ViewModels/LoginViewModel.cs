using System.ComponentModel.DataAnnotations;

namespace OAuthDoZero.Server.ViewModels;

/// <summary>
/// ViewModel para página de login
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Nome de usuário é obrigatório")]
    [Display(Name = "Nome de usuário")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    [Display(Name = "Senha")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Lembrar-me")]
    public bool RememberMe { get; set; }

    // Parâmetros OAuth para manter o estado
    public string? ReturnUrl { get; set; }
    public string? ClientId { get; set; }
    public string? RedirectUri { get; set; }
    public string? ResponseType { get; set; }
    public string? State { get; set; }
    public string? Scope { get; set; }
    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; }
    public string? Nonce { get; set; }

    // Para exibição
    public string? ErrorMessage { get; set; }
    public string? ClientName { get; set; }
}