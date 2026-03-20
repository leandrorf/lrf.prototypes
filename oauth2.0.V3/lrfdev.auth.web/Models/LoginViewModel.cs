using System.ComponentModel.DataAnnotations;

namespace lrfdev.auth.web.Models;

public sealed class LoginViewModel
{
    [Required]
    [Display(Name = "Usuário")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    public string? AccessToken { get; set; }
    public IReadOnlyCollection<string> Permissions { get; set; } = [];
    public string? ErrorMessage { get; set; }
}
