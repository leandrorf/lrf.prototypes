namespace identityserver.api.Configuration;

/// <summary>URL base do projeto Web que exibe a tela de login (para redirecionamento a partir do GET /connect/authorize).</summary>
public class LoginUiOptions
{
    public const string SectionName = "LoginUi";

    /// <summary>Ex.: http://localhost:5225</summary>
    public string BaseUrl { get; set; } = "http://localhost:5225";

    public string SignInPath => BaseUrl.TrimEnd('/') + "/Account/SignIn";
}
