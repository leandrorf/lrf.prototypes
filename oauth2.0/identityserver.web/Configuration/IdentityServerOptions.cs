namespace identityserver.web.Configuration;

/// <summary>URL base do IdentityServer (API) para onde o formulário de login envia o POST.</summary>
public class IdentityServerOptions
{
    public const string SectionName = "IdentityServer";

    /// <summary>Ex.: http://localhost:5235</summary>
    public string Authority { get; set; } = "http://localhost:5235";

    public string AuthorizeEndpoint => Authority.TrimEnd('/') + "/connect/authorize";
}
