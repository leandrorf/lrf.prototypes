namespace lrfdev.auth.api.Models;

/// <summary>
/// Sessão do OAuth 2.0 Device Authorization Grant (RFC 8628).
/// </summary>
public sealed class OAuthDeviceFlowSession
{
    public Guid Id { get; set; }

    /// <summary>Segredo de longa entropia (não exibir ao usuário).</summary>
    public string DeviceCode { get; set; } = string.Empty;

    /// <summary>Código curto exibido na TV / digitado no celular (ex.: ABCD-EFGH).</summary>
    public string UserCode { get; set; } = string.Empty;

    public Guid ClientId { get; set; }
    public OAuthClient Client { get; set; } = default!;

    public string Scope { get; set; } = string.Empty;

    /// <summary>pending | approved | denied | consumed</summary>
    public string Status { get; set; } = "pending";

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Intervalo mínimo sugerido entre polls (segundos).</summary>
    public int IntervalSeconds { get; set; } = 5;

    public DateTime? LastPollAtUtc { get; set; }
}
