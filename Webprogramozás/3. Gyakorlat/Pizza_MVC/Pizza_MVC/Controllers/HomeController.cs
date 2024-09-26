using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pizza_MVC.Models;
using Pizza_MVC.Services;

namespace Pizza_MVC.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IPizzaService _pizzaService;

    public HomeController(ILogger<HomeController> logger, IPizzaService pizzaService)
    {
        _logger = logger;
        _pizzaService = pizzaService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

