using System.Security.Claims;
using System.Text.Json;

namespace lrfdev.auth.api.Authorization;

/// <summary>
/// Valida escopos/permissões vindos do access token (usuário ou client_credentials).
/// Aceita claim <c>permission</c> (repetida, ou valor JSON <c>["a","b"]</c> em um único claim)
/// e/ou claim <c>scope</c> (espaços; pode haver mais de um claim <c>scope</c>).
/// </summary>
public static class ScopeAuthorizationEvaluator
{
    public static bool HasScope(ClaimsPrincipal user, string requiredScope)
    {
        foreach (var claim in user.FindAll("permission"))
        {
            if (string.Equals(claim.Value, requiredScope, StringComparison.Ordinal))
            {
                return true;
            }

            if (TryParsePermissionJsonArray(claim.Value, out var list)
                && list.Contains(requiredScope, StringComparer.Ordinal))
            {
                return true;
            }
        }

        var scopeParts = new List<string>();
        foreach (var claim in user.FindAll("scope"))
        {
            if (string.IsNullOrWhiteSpace(claim.Value))
            {
                continue;
            }

            scopeParts.AddRange(
                claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return scopeParts.Contains(requiredScope, StringComparer.Ordinal);
    }

    private static bool TryParsePermissionJsonArray(string value, out string[] items)
    {
        items = Array.Empty<string>();
        var trimmed = value.Trim();
        if (trimmed.Length < 2 || trimmed[0] != '[' || trimmed[^1] != ']')
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<string[]>(trimmed);
            if (parsed is { Length: > 0 })
            {
                items = parsed;
                return true;
            }
        }
        catch (JsonException)
        {
            // ignore
        }

        return false;
    }

    public static string PolicyName(string scope) => $"Scope:{scope}";
}
