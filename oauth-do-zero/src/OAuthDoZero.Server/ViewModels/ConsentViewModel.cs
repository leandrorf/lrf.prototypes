namespace OAuthDoZero.Server.ViewModels;

/// <summary>
/// ViewModel para página de consentimento OAuth
/// </summary>
public class ConsentViewModel
{
    // Informações do usuário
    public string Username { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;

    // Informações do cliente
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string? ClientDescription { get; set; }

    // Escopos solicitados
    public List<ScopeInfo> RequestedScopes { get; set; } = new();

    // Parâmetros OAuth para manter estado
    public string AuthorizationCode { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string? State { get; set; }
    public string ResponseType { get; set; } = string.Empty;
    public string CodeChallenge { get; set; } = string.Empty;
    public string CodeChallengeMethod { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new List<string>();

    // Campos do formulário
    public List<string> SelectedScopes { get; set; } = new();
    public bool RememberConsent { get; set; }
}

/// <summary>
/// Informações de um escopo para exibição
/// </summary>
public class ScopeInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Required { get; set; }
    public bool Emphasize { get; set; }
}