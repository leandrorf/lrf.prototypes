using Microsoft.AspNetCore.Mvc;
using OAuthDoZero.Server.DTOs;
using OAuthDoZero.Server.Services;
using System.Web;

namespace OAuthDoZero.Server.Controllers;

/// <summary>
/// Controller para endpoint de autorização OAuth 2.0 / OpenID Connect
/// </summary>
[ApiController]
[Route("oauth")]
public class AuthorizationController : ControllerBase
{
    private readonly IOAuthService _oauthService;
    private readonly ICryptographyService _cryptoService;

    public AuthorizationController(IOAuthService oauthService, ICryptographyService cryptoService)
    {
        _oauthService = oauthService;
        _cryptoService = cryptoService;
    }

    /// <summary>
    /// Endpoint de autorização OAuth 2.0 / OpenID Connect
    /// </summary>
    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize([FromQuery] AuthorizationRequest request)
    {
        try
        {
            // Validar parâmetros obrigatórios
            if (string.IsNullOrEmpty(request.ResponseType) || request.ResponseType != "code")
                return BadRequest(CreateErrorResponse("unsupported_response_type", "Only 'code' response type is supported", request.State));

            if (string.IsNullOrEmpty(request.ClientId))
                return BadRequest(CreateErrorResponse("invalid_request", "client_id is required", request.State));

            if (string.IsNullOrEmpty(request.RedirectUri))
                return BadRequest(CreateErrorResponse("invalid_request", "redirect_uri is required", request.State));

            // Validar cliente
            var client = await _oauthService.ValidateClientAsync(request.ClientId);
            if (client == null)
                return BadRequest(CreateErrorResponse("invalid_client", "Invalid client_id", request.State));

            // Validar redirect URI
            if (!await _oauthService.ValidateRedirectUriAsync(request.ClientId, request.RedirectUri))
                return BadRequest(CreateErrorResponse("invalid_request", "Invalid redirect_uri", request.State));

            // Validar PKCE se requerido
            if (client.RequirePkce)
            {
                if (string.IsNullOrEmpty(request.CodeChallenge))
                    return BadRequest(CreateErrorResponse("invalid_request", "code_challenge is required for this client", request.State));

                if (request.CodeChallengeMethod != "S256" && !client.AllowPlainTextPkce)
                    return BadRequest(CreateErrorResponse("invalid_request", "code_challenge_method must be S256", request.State));
            }

            // Validar scopes
            var requestedScopes = string.IsNullOrEmpty(request.Scope) ? 
                new[] { "openid" } : 
                request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var allowedScopes = client.AllowedScopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var invalidScopes = requestedScopes.Except(allowedScopes);

            if (invalidScopes.Any())
                return BadRequest(CreateErrorResponse("invalid_scope", $"Invalid scopes: {string.Join(", ", invalidScopes)}", request.State));

            // Por enquanto, vamos simular um usuário logado
            // Em uma implementação real, aqui seria verificado se o usuário está logado
            // e redirecionado para login se necessário
            var user = await GetCurrentUserAsync(); // Método que vamos implementar
            if (user == null)
            {
                // Redirecionar para login com parâmetros de retorno
                var loginUrl = $"/login?return_url={HttpUtility.UrlEncode(Request.QueryString.ToString())}";
                return Redirect(loginUrl);
            }

            // Gerar authorization code
            var authCode = await _oauthService.CreateAuthorizationCodeAsync(user, client, request);

            // Construir URL de redirecionamento
            var redirectUrl = BuildRedirectUrl(request.RedirectUri, authCode.Code, request.State);
            
            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            return BadRequest(CreateErrorResponse("server_error", "Internal server error", request.State));
        }
    }

    private async Task<Models.User?> GetCurrentUserAsync()
    {
        // TODO: Implementar autenticação real
        // Por enquanto, retorna um usuário fake para teste
        return await _oauthService.GetUserByUsernameAsync("demo");
    }

    private OAuthErrorResponse CreateErrorResponse(string error, string description, string? state = null)
    {
        return new OAuthErrorResponse
        {
            Error = error,
            ErrorDescription = description,
            State = state
        };
    }

    private string BuildRedirectUrl(string baseUrl, string code, string? state)
    {
        var uriBuilder = new UriBuilder(baseUrl);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        query["code"] = code;
        
        if (!string.IsNullOrEmpty(state))
            query["state"] = state;

        uriBuilder.Query = query.ToString();
        return uriBuilder.ToString();
    }
}