using Microsoft.AspNetCore.Mvc;
using OAuthDoZero.Server.ViewModels;
using OAuthDoZero.Server.Services;
using System.Web;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace OAuthDoZero.Server.Controllers;

/// <summary>
/// Controller para autenticação de usuários
/// </summary>
public class AccountController : Controller
{
    private readonly IOAuthService _oauthService;
    private readonly ICryptographyService _cryptoService;

    public AccountController(IOAuthService oauthService, ICryptographyService cryptoService)
    {
        _oauthService = oauthService;
        _cryptoService = cryptoService;
    }

    /// <summary>
    /// Página de login
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        // Verificar se usuário já está logado
        if (User.Identity?.IsAuthenticated == true)
        {
            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);
            
            return RedirectToAction("Index", "Home");
        }

        var model = new LoginViewModel
        {
            ReturnUrl = returnUrl
        };

        // Extrair parâmetros OAuth da URL de retorno se presente
        if (!string.IsNullOrEmpty(returnUrl))
        {
            var uri = new Uri($"http://localhost{returnUrl}");
            var query = HttpUtility.ParseQueryString(uri.Query);

            model.ClientId = query["client_id"];
            model.RedirectUri = query["redirect_uri"];
            model.State = query["state"];
            model.Scope = query["scope"];
            model.CodeChallenge = query["code_challenge"];
            model.CodeChallengeMethod = query["code_challenge_method"];
            model.Nonce = query["nonce"];

            // Buscar nome do cliente para exibição
            if (!string.IsNullOrEmpty(model.ClientId))
            {
                var client = await _oauthService.ValidateClientAsync(model.ClientId);
                model.ClientName = client?.ClientName ?? "Aplicação desconhecida";
            }
        }

        return View(model);
    }

    /// <summary>
    /// Processar login
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            // Validar credenciais
            var isValid = await _oauthService.ValidateUserPasswordAsync(model.Username, model.Password);
            if (!isValid)
            {
                model.ErrorMessage = "Nome de usuário ou senha inválidos.";
                return View(model);
            }

            // Buscar usuário
            var user = await _oauthService.GetUserByUsernameAsync(model.Username);
            if (user == null || !user.IsActive)
            {
                model.ErrorMessage = "Usuário não encontrado ou inativo.";
                return View(model);
            }

            // Criar claims de autenticação
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email),
                new("preferred_username", user.PreferredUsername ?? user.Username)
            };

            if (!string.IsNullOrEmpty(user.FirstName))
                claims.Add(new("given_name", user.FirstName));
            
            if (!string.IsNullOrEmpty(user.LastName))
                claims.Add(new("family_name", user.LastName));

            // Criar identity e principal
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Configurar propriedades de autenticação
            var authProps = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? 
                    DateTimeOffset.UtcNow.AddDays(30) : 
                    DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true
            };

            // Fazer login do usuário
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

            // Atualizar último login
            user.LastLoginAt = DateTime.UtcNow;
            // TODO: Salvar no contexto

            // Redirecionar
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            model.ErrorMessage = "Erro interno do servidor. Tente novamente.";
            return View(model);
        }
    }

    /// <summary>
    /// Logout
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Página de acesso negado
    /// </summary>
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}