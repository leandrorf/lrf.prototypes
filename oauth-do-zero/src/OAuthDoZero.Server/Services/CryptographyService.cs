using System.Security.Cryptography;
using System.Text;

namespace OAuthDoZero.Server.Services;

/// <summary>
/// Serviço para operações de segurança e criptografia
/// </summary>
public interface ICryptographyService
{
    string HashPassword(string password, string salt);
    string GenerateSalt();
    bool VerifyPassword(string password, string salt, string hashedPassword);
    string GenerateSecureRandomString(int length = 32);
    string ComputeSha256Hash(string input);
    bool VerifyCodeChallenge(string codeVerifier, string codeChallenge, string method);
}

public class CryptographyService : ICryptographyService
{
    public string HashPassword(string password, string salt)
    {
        return BCrypt.Net.BCrypt.HashPassword(password + salt);
    }

    public string GenerateSalt()
    {
        return BCrypt.Net.BCrypt.GenerateSalt();
    }

    public bool VerifyPassword(string password, string salt, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password + salt, hashedPassword);
    }

    public string GenerateSecureRandomString(int length = 32)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "")
            .Substring(0, length);
    }

    public string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    /// <summary>
    /// Verifica o code challenge do PKCE
    /// </summary>
    public bool VerifyCodeChallenge(string codeVerifier, string codeChallenge, string method)
    {
        if (method == "S256")
        {
            var hashedVerifier = ComputeSha256Hash(codeVerifier);
            return hashedVerifier == codeChallenge;
        }
        else if (method == "plain")
        {
            return codeVerifier == codeChallenge;
        }

        return false;
    }
}