namespace identityserver.web.Configuration;

/// <summary>URL pública do site (para QR code, links no celular). Se vazio, usa o host da requisição.</summary>
public class AppOptions
{
    public const string SectionName = "App";

    /// <summary>Ex.: http://192.168.0.10:5225 — use o IPv4 do PC na rede para o celular acessar.</summary>
    public string? PublicBaseUrl { get; set; }
}
