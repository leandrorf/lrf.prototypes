namespace identityserver.web.Models;

public class TvLoginScanViewModel
{
    public string SessionId { get; set; } = "";
    public string? UserName { get; set; }
    public string? Error { get; set; }
}
