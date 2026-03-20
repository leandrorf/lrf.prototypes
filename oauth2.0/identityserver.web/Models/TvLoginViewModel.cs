namespace identityserver.web.Models;

public class TvLoginViewModel
{
    public string SessionId { get; set; } = "";
    public string ScanUrl { get; set; } = "";
    /// <summary>Dispositivo vinculado à sessão (query <c>device_id</c> na TV).</summary>
    public string? DeviceExternalId { get; set; }
}
