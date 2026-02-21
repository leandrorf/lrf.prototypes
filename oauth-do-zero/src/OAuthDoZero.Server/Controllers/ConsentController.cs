using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OAuthDoZero.Server.ViewModels;
using OAuthDoZero.Server.Services;
using OAuthDoZero.Server.Data;
using Microsoft.EntityFrameworkCore;
using System.Web;

namespace OAuthDoZero.Server.Controllers;

/// <summary>
/// Controller para página de consentimento OAuth
/// </summary>
[Authorize]
public class ConsentController : Controller
{
    private readonly IOAuthService _oauthService;
    private readonly OAuthDbContext _context;

    public ConsentController(IOAuthService oauthService, OAuthDbContext context)
    {
        _oauthService = oauthService;
        _context = context;
    }

    /// <summary>
    /// Exibir página de consentimento
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? returnUrl = null)
    {
        if (string.IsNullOrEmpty(returnUrl))
            return BadRequest("URL de retorno é obrigatória");

        try
        {
            // Extrair parâmetros OAuth da URL
            var uri = new Uri($"http://localhost{returnUrl}");
            var query = HttpUtility.ParseQueryString(uri.Query);

            var clientId = query["client_id"];
            var redirectUri = query["redirect_uri"];
            var scopes = query["scope"];
            var state = query["state"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
                return BadRequest("Parâmetros OAuth inválidos");

            // Validar cliente
            var client = await _oauthService.ValidateClientAsync(clientId);
            if (client == null)
                return BadRequest("Cliente inválido");

            // Buscar informações dos escopos
            var requestedScopes = string.IsNullOrEmpty(scopes) ? 
                new[] { "openid" } : 
                scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var scopeInfos = await _context.Scopes
                .Where(s => requestedScopes.Contains(s.Name) && s.IsActive)
                .Select(s => new ScopeInfo
                {
                    Name = s.Name,
                    DisplayName = s.DisplayName,
                    Description = s.Description,
                    Required = s.Required,
                    Emphasize = s.Emphasize
                })
                .ToListAsync();

            // Obter informações do usuário
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var user = await _oauthService.GetUserByIdAsync(userId!);

            if (user == null)
                return Challenge(); // Forçar novo login

            var model = new ConsentViewModel
            {
                Username = user.Username,
                UserDisplayName = $"{user.FirstName} {user.LastName}".Trim() ?? user.Username,
                ClientId = client.ClientId,
                ClientName = client.ClientName,
                ClientDescription = client.Description,
                RequestedScopes = scopeInfos,
                RedirectUri = redirectUri,
                State = state,
                // Pre-selecionar escopos obrigatórios
                SelectedScopes = scopeInfos.Where(s => s.Required).Select(s => s.Name).ToList()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            return BadRequest("Erro ao processar solicitação de consentimento");
        }
    }

    /// <summary>
    /// Processar consentimento do usuário
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Grant(ConsentViewModel model, string action)
    {
        try
        {
            if (action == "deny")
            {
                // Usuário negou o consentimento
                var denyUrl = BuildErrorRedirectUrl(model.RedirectUri, "access_denied", 
                    "Usuário negou o consentimento", model.State);
                return Redirect(denyUrl);
            }

            // Validar escopos selecionados
            if (!model.SelectedScopes.Any())
            {
                ModelState.AddModelError("", "Você deve conceder pelo menos um escopo");
                return await Index($"/oauth/authorize?client_id={model.ClientId}&redirect_uri={model.RedirectUri}&state={model.State}");
            }

            // Buscar usuário atual
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var user = await _oauthService.GetUserByIdAsync(userId!);
            var client = await _oauthService.ValidateClientAsync(model.ClientId);

            if (user == null || client == null)
                return BadRequest("Usuário ou cliente inválido");

            // Construir request OAuth simulado para criar authorization code
            var authRequest = new DTOs.AuthorizationRequest
            {
                ResponseType = "code",
                ClientId = model.ClientId,
                RedirectUri = model.RedirectUri,
                Scope = string.Join(" ", model.SelectedScopes),
                State = model.State
            };

            // Criar authorization code
            var authCode = await _oauthService.CreateAuthorizationCodeAsync(user, client, authRequest);

            // Construir URL de redirecionamento com código
            var redirectUrl = BuildSuccessRedirectUrl(model.RedirectUri, authCode.Code, model.State);
            
            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            var errorUrl = BuildErrorRedirectUrl(model.RedirectUri, "server_error", 
                "Erro interno do servidor", model.State);
            return Redirect(errorUrl);
        }
    }

    private string BuildSuccessRedirectUrl(string baseUrl, string code, string? state)
    {
        var uriBuilder = new UriBuilder(baseUrl);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        query["code"] = code;
        
        if (!string.IsNullOrEmpty(state))
            query["state"] = state;

        uriBuilder.Query = query.ToString();
        return uriBuilder.ToString();
    }

    private string BuildErrorRedirectUrl(string baseUrl, string error, string description, string? state)
    {
        var uriBuilder = new UriBuilder(baseUrl);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        query["error"] = error;
        query["error_description"] = description;
        
        if (!string.IsNullOrEmpty(state))
            query["state"] = state;

        uriBuilder.Query = query.ToString();
        return uriBuilder.ToString();
    }
}