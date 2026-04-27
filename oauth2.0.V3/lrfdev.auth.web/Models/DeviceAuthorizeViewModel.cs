using System.ComponentModel.DataAnnotations;

namespace lrfdev.auth.web.Models;

public sealed class DeviceAuthorizeViewModel
{
    [Required]
    [Display(Name = "Código do dispositivo")]
    public string UserCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Usuário")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
