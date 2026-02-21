using Microsoft.AspNetCore.Mvc;
using OAuthDoZero.Server.DTOs;
using OAuthDoZero.Server.Services;
using OAuthDoZero.Server.Models;

namespace OAuthDoZero.Server.Controllers;

/// <summary>
/// Controller para endpoint de token OAuth 2.0
/// </summary>
[ApiController]
[Route("oauth")]
public class TokenController : ControllerBase
{
    private readonly IOAuthService _oauthService;
    private readonly IJwtService _jwtService;
    private readonly ICryptographyService _cryptoService;

    public TokenController(IOAuthService oauthService, IJwtService jwtService, ICryptographyService cryptoService)
    {
        _oauthService = oauthService;
        _jwtService = jwtService;
        _cryptoService = cryptoService;
    }

    /// <summary>
    /// Endpoint para trocar authorization code por tokens
    /// </summary>
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token([FromForm] TokenRequest request)
    {
        try
        {
            return request.GrantType switch
            {
                "authorization_code" => await HandleAuthorizationCodeGrant(request),
                "refresh_token" => await HandleRefreshTokenGrant(request),
                "client_credentials" => await HandleClientCredentialsGrant(request),
                _ => BadRequest(CreateErrorResponse("unsupported_grant_type", "Grant type not supported"))
            };
        }
        catch (Exception ex)
        {
            return BadRequest(CreateErrorResponse("server_error", "Internal server error"));
        }
    }

    private async Task<IActionResult> HandleAuthorizationCodeGrant(TokenRequest request)
    {
        // Validar parâmetros obrigatórios
        if (string.IsNullOrEmpty(request.Code))
            return BadRequest(CreateErrorResponse("invalid_request", "code is required"));

        if (string.IsNullOrEmpty(request.ClientId))
            return BadRequest(CreateErrorResponse("invalid_request", "client_id is required"));

        if (string.IsNullOrEmpty(request.RedirectUri))
            return BadRequest(CreateErrorResponse("invalid_request", "redirect_uri is required"));

        // Validar cliente
        var client = await _oauthService.ValidateClientAsync(request.ClientId, request.ClientSecret);
        if (client == null)
            return BadRequest(CreateErrorResponse("invalid_client", "Invalid client credentials"));

        // Buscar authorization code
        var authCode = await _oauthService.GetAuthorizationCodeAsync(request.Code);
        if (authCode == null)
            return BadRequest(CreateErrorResponse("invalid_grant", "Invalid or expired authorization code"));

        // Validar se o code pertence ao cliente
        if (authCode.ClientId != request.ClientId)
            return BadRequest(CreateErrorResponse("invalid_grant", "Authorization code does not belong to client"));

        // Validar redirect URI
        if (authCode.RedirectUri != request.RedirectUri)
            return BadRequest(CreateErrorResponse("invalid_grant", "Redirect URI mismatch"));

        // Validar PKCE se presente
        if (!string.IsNullOrEmpty(authCode.CodeChallenge))
        {
            if (string.IsNullOrEmpty(request.CodeVerifier))
                return BadRequest(CreateErrorResponse("invalid_request", "code_verifier is required"));

            if (!_cryptoService.VerifyCodeChallenge(request.CodeVerifier, authCode.CodeChallenge, authCode.CodeChallengeMethod ?? "S256"))
                return BadRequest(CreateErrorResponse("invalid_grant", "Invalid code_verifier"));
        }

        // Marcar code como usado
        authCode.IsUsed = true;
        authCode.UsedAt = DateTime.UtcNow;
        
        // Obter scopes
        var scopes = authCode.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Gerar access token
        var accessToken = await _oauthService.CreateAccessTokenAsync(authCode.User, client, scopes);
        var jwtAccessToken = _jwtService.GenerateAccessToken(authCode.User, client, scopes, TimeSpan.FromSeconds(client.AccessTokenLifetime));

        // Gerar refresh token se permitido
        Models.RefreshToken? refreshToken = null;
        if (client.AllowRefreshToken && scopes.Contains("offline_access"))
        {
            refreshToken = await _oauthService.CreateRefreshTokenAsync(accessToken);
        }

        // Gerar ID token se OpenID Connect
        string? idToken = null;
        if (scopes.Contains("openid"))
        {
            idToken = _jwtService.GenerateIdToken(authCode.User, client, scopes, authCode.Nonce ?? "", TimeSpan.FromSeconds(client.AccessTokenLifetime));
        }

        var response = new TokenResponse
        {
            AccessToken = jwtAccessToken,
            TokenType = "Bearer",
            ExpiresIn = client.AccessTokenLifetime,
            RefreshToken = refreshToken is not null ? GenerateRefreshTokenValue(refreshToken) : null,
            Scope = string.Join(" ", scopes),
            IdToken = idToken
        };

        return Ok(response);
    }

    private async Task<IActionResult> HandleRefreshTokenGrant(TokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            return BadRequest(CreateErrorResponse("invalid_request", "refresh_token is required"));

        if (string.IsNullOrEmpty(request.ClientId))
            return BadRequest(CreateErrorResponse("invalid_request", "client_id is required"));

        // Validar cliente
        var client = await _oauthService.ValidateClientAsync(request.ClientId, request.ClientSecret);
        if (client == null)
            return BadRequest(CreateErrorResponse("invalid_client", "Invalid client credentials"));

        // TODO: Implementar validação de refresh token
        // Por enquanto retornamos erro
        return BadRequest(CreateErrorResponse("invalid_grant", "Refresh token implementation pending"));
    }

    /// <summary>
    /// Handle Client Credentials Grant para autenticação machine-to-machine
    /// </summary>
    private async Task<IActionResult> HandleClientCredentialsGrant(TokenRequest request)
    {
        // Validar parâmetros obrigatórios
        if (string.IsNullOrEmpty(request.ClientId))
            return BadRequest(CreateErrorResponse("invalid_request", "client_id is required"));

        if (string.IsNullOrEmpty(request.ClientSecret))
            return BadRequest(CreateErrorResponse("invalid_request", "client_secret is required"));

        // Validar cliente
        var client = await _oauthService.ValidateClientAsync(request.ClientId, request.ClientSecret);
        if (client == null)
            return BadRequest(CreateErrorResponse("invalid_client", "Invalid client credentials"));

        // Verificar se o cliente suporta client_credentials grant
        var allowedGrantTypes = client.GrantTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(g => g.Trim()).ToList();
        
        if (!allowedGrantTypes.Contains("client_credentials"))
            return BadRequest(CreateErrorResponse("unauthorized_client", "Client not authorized for client_credentials grant"));

        // Verificar se é um cliente confidencial
        if (client.ClientType != "confidential")
            return BadRequest(CreateErrorResponse("invalid_client", "Client credentials grant requires confidential client"));

        // Determinar scopes permitidos (usar scopes solicitados ou padrão do cliente)
        var requestedScopes = !string.IsNullOrEmpty(request.Scope) 
            ? request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            : new string[0];
        
        var allowedScopes = client.AllowedScopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Se nenhum scope foi solicitado, usar todos os permitidos (exceto openid para M2M)
        var scopes = requestedScopes.Length > 0 
            ? requestedScopes.Where(s => allowedScopes.Contains(s) && s != "openid").ToArray()
            : allowedScopes.Where(s => s != "openid").ToArray();

        if (!scopes.Any())
            return BadRequest(CreateErrorResponse("invalid_scope", "No valid scopes requested or allowed"));

        // Cliente confidental não precisa de usuário para M2M, vamos criar um "usuário" de serviço
        // ou passar null/especial para indicar que é machine-to-machine
        var jwtAccessToken = _jwtService.GenerateAccessToken(
            user: null, // M2M não tem usuário específico
            client: client, 
            scopes: scopes, 
            expiry: TimeSpan.FromSeconds(client.AccessTokenLifetime));

        var response = new TokenResponse
        {
            AccessToken = jwtAccessToken,
            TokenType = "Bearer",
            ExpiresIn = client.AccessTokenLifetime,
            Scope = string.Join(" ", scopes)
            // Não incluir refresh_token nem id_token para client_credentials
        };

        return Ok(response);
    }

    private string GenerateRefreshTokenValue(Models.RefreshToken refreshToken)
    {
        // Em uma implementação real, retornaríamos o valor original do refresh token
        // Por agora, geramos um novo valor baseado no hash
        return _cryptoService.GenerateSecureRandomString(64);
    }

    private OAuthErrorResponse CreateErrorResponse(string error, string description)
    {
        return new OAuthErrorResponse
        {
            Error = error,
            ErrorDescription = description
        };
    }
}