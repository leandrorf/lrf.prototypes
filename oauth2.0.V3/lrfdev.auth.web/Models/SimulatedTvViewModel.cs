namespace lrfdev.auth.web.Models;

public sealed class SimulatedTvViewModel
{
    public string UserCode { get; set; } = string.Empty;
    public string VerificationUri { get; set; } = string.Empty;
    public string VerificationUriComplete { get; set; } = string.Empty;
    public string QrDataUrl { get; set; } = string.Empty;
    public int PollIntervalSeconds { get; set; } = 5;
    public string? ErrorMessage { get; set; }
}
