using Beadando.Data;
using Beadando.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Beadando.Controllers;
public class LeagueController : Controller
{
    private readonly SportsDbContext _context;

    public LeagueController(SportsDbContext context)
    {
        _context = context;
    }

    public IActionResult Select()
    {
        int? preferredLeagueId = HttpContext.Session.GetInt32("PreferredLeagueId");

        var leagues = _context.Leagues
            .OrderBy(l => l.Name)
            .Select(l => new LeagueSelectionViewModel
            {
                Id = l.Id,
                Name = l.Name,
                IsSelected = preferredLeagueId.HasValue && l.Id == preferredLeagueId.Value // Mark as selected if it matches
            })
            .ToList();

        return View(leagues);
    }

    [HttpPost]
    public IActionResult Select(int selectedLeagueId)
    {
        HttpContext.Session.SetInt32("PreferredLeagueId", selectedLeagueId);

        return RedirectToAction("Index", "Home");
    }
}
