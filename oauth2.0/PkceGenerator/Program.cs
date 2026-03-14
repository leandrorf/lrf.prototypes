using System.Security.Cryptography;
using System.Text;

// Gera code_verifier e code_challenge (S256) para PKCE - RFC 7636
// code_verifier: 43-128 caracteres [A-Za-z0-9\-._~]
// code_challenge: Base64URL(SHA256(code_verifier))

var verifier = GenerateCodeVerifier();
var challenge = ComputeS256Challenge(verifier);

Console.WriteLine("=== PKCE (RFC 7636) ===");
Console.WriteLine("Antes de abrir o link: suba a API Identity Server (dotnet run --project IdentityServer).\n");
Console.WriteLine("code_verifier (guarde para o POST /connect/token):");
Console.WriteLine(verifier);
Console.WriteLine();
Console.WriteLine("code_challenge (use na URL do /connect/authorize):");
Console.WriteLine(challenge);
Console.WriteLine();
Console.WriteLine("--- URL de autorização (exemplo) ---");
Console.WriteLine("Substitua {baseUrl}, {clientId} e {redirectUri} pelos valores do seu cliente (cadastrado no banco).");
var baseUrl = "http://localhost:5235";
var clientId = "meu-app";
var redirectUri = "https://app.example.com/callback";
var authUrl = $"{baseUrl}/connect/authorize?client_id={Uri.EscapeDataString(clientId)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&state=xyz&code_challenge={Uri.EscapeDataString(challenge)}&code_challenge_method=S256";
Console.WriteLine(authUrl);
Console.WriteLine();
Console.WriteLine("No POST /connect/token envie: code_verifier = (valor acima), redirect_uri = o mesmo usado acima.");

static string GenerateCodeVerifier()
{
    // 32 bytes => 43 caracteres em Base64URL (dentro do range 43-128)
    var bytes = RandomNumberGenerator.GetBytes(32);
    return ToBase64Url(bytes);
}

static string ComputeS256Challenge(string codeVerifier)
{
    var bytes = Encoding.ASCII.GetBytes(codeVerifier);
    var hash = SHA256.HashData(bytes);
    return ToBase64Url(hash);
}

static string ToBase64Url(byte[] data)
{
    return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
