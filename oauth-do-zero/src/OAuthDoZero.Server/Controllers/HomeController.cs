using Microsoft.AspNetCore.Mvc;

namespace OAuthDoZero.Server.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
