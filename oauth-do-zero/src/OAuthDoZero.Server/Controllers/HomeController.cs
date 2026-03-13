using Microsoft.AspNetCore.Mvc;

namespace OAuthDoZero.Server.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult DeviceDemo()
    {
        return View();
    }
}
