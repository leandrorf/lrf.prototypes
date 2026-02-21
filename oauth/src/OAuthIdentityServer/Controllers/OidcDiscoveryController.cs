using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuthIdentityServer.Configuration;
using OAuthIdentityServer.Services;

namespace OAuthIdentityServer.Controllers;

/// <summary>
/// OpenID Connect Discovery e JWKS.
/// </summary>
[ApiController]
public class OidcDiscoveryController : ControllerBase
{
    private readonly OAuthOptions _options;
    private readonly ITokenService _tokenService;

    public OidcDiscoveryController(IOptions<OAuthOptions> options, ITokenService tokenService)
    {
        _options = options.Value;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Documento de descoberta OIDC: /.well-known/openid-configuration
    /// </summary>
    [HttpGet(".well-known/openid-configuration")]
    [HttpGet("oauth/.well-known/openid-configuration")]
    public IActionResult OpenIdConfiguration()
    {
        var issuer = _options.Issuer.TrimEnd('/');
        var baseUrl = issuer;

        var doc = new
        {
            issuer = issuer,
            authorization_endpoint = $"{baseUrl}/oauth/authorize",
            token_endpoint = $"{baseUrl}/oauth/token",
            userinfo_endpoint = $"{baseUrl}/oauth/userinfo",
            jwks_uri = $"{baseUrl}/oauth/jwks",
            scopes_supported = new[] { "openid", "profile", "email", "offline_access" },
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code", "refresh_token" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "HS256", "RS256" },
            token_endpoint_auth_methods_supported = new[] { "client_secret_post", "client_secret_basic", "none" },
            code_challenge_methods_supported = new[] { "S256", "plain" }
        };
        return Ok(doc);
    }

    /// <summary>
    /// Chaves públicas para validação de assinatura (JWKS). Este servidor usa HS256 com chave simétrica;
    /// em produção use RS256 e exponha a chave pública aqui.
    /// </summary>
    [HttpGet("oauth/jwks")]
    public IActionResult Jwks()
    {
        var keyBytes = _tokenService.GetSigningKeyBytes();
        var keyBase64 = Convert.ToBase64String(keyBytes);
        var jwk = new
        {
            kty = "oct",
            use = "sig",
            alg = "HS256",
            k = keyBase64
        };
        return Ok(new { keys = new[] { jwk } });
    }
}
