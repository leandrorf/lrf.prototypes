using Microsoft.AspNetCore.Mvc;
using lrfdev.auth.web.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace lrfdev.auth.web.Controllers;

public sealed class AccountController(IHttpClientFactory httpClientFactory) : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = httpClientFactory.CreateClient("auth-api");
        var payload = new
        {
            username = model.Username,
            password = model.Password
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", payload);

        if (!response.IsSuccessStatusCode)
        {
            model.ErrorMessage = "Usuário ou senha inválidos.";
            return View(model);
        }

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginApiResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (loginResponse is null)
        {
            model.ErrorMessage = "Falha ao processar o retorno da API.";
            return View(model);
        }

        model.AccessToken = loginResponse.AccessToken;
        model.Permissions = loginResponse.Permissions;
        model.Password = string.Empty;

        return View(model);
    }
}
