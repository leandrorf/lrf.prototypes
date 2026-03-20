namespace identityserver.web.Services;

/// <summary>Armazena sessões de login via QR (TV) em memória. Chave = session_id.</summary>
public interface ITvSessionStore
{
    /// <param name="deviceExternalId">Opcional: ID do dispositivo (ex. tv-001) enviado ao login da API para validação por grupo.</param>
    string CreateSession(string? deviceExternalId = null);
    void SetTokens(string sessionId, TvSessionTokens tokens);
    TvSessionResult? Get(string sessionId);
}

public sealed class TvSessionTokens
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public string IdToken { get; set; } = "";
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
}

public sealed class TvSessionResult
{
    public const string Pending = "pending";
    public const string Completed = "completed";

    public string Status { get; set; } = Pending;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? IdToken { get; set; }
    public string? TokenType { get; set; }
    public int? ExpiresIn { get; set; }
    public string? DeviceExternalId { get; set; }
}

public sealed class TvSessionStore : ITvSessionStore
{
    private static readonly TimeSpan SessionExpiry = TimeSpan.FromMinutes(10);
    private readonly Dictionary<string, (TvSessionTokens? tokens, DateTime createdAt, string? deviceExternalId)> _sessions = new();
    private readonly object _lock = new();

    public string CreateSession(string? deviceExternalId = null)
    {
        var sessionId = Guid.NewGuid().ToString("N")[..12];
        var device = string.IsNullOrWhiteSpace(deviceExternalId) ? null : deviceExternalId.Trim();
        lock (_lock)
        {
            _sessions[sessionId] = (null, DateTime.UtcNow, device);
            CleanupLocked();
        }
        return sessionId;
    }

    public void SetTokens(string sessionId, TvSessionTokens tokens)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(sessionId, out var entry))
                _sessions[sessionId] = (tokens, entry.createdAt, entry.deviceExternalId);
        }
    }

    public TvSessionResult? Get(string sessionId)
    {
        lock (_lock)
        {
            CleanupLocked();
            if (!_sessions.TryGetValue(sessionId, out var entry))
                return null;
            if (entry.tokens is null)
                return new TvSessionResult { Status = TvSessionResult.Pending, DeviceExternalId = entry.deviceExternalId };
            return new TvSessionResult
            {
                Status = TvSessionResult.Completed,
                AccessToken = entry.tokens.AccessToken,
                RefreshToken = entry.tokens.RefreshToken,
                IdToken = entry.tokens.IdToken,
                TokenType = entry.tokens.TokenType,
                ExpiresIn = entry.tokens.ExpiresIn,
                DeviceExternalId = entry.deviceExternalId
            };
        }
    }

    private void CleanupLocked()
    {
        var cutoff = DateTime.UtcNow - SessionExpiry;
        foreach (var k in _sessions.Where(p => p.Value.createdAt < cutoff).Select(p => p.Key).ToList())
            _sessions.Remove(k);
    }
}
