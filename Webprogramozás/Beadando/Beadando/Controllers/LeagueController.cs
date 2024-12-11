using Beadando.Data;
using Beadando.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Beadando.Controllers;
public class LeagueController : Controller
{
    private readonly SportsDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeagueController(SportsDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Select()
    {
        int? preferredLeagueId = null;

        if (User.Identity.IsAuthenticated)
        {
            
            var user = _userManager.GetUserAsync(User).Result;
            preferredLeagueId = user?.FavoriteLeagueId;
        }
        else
        {
            
            preferredLeagueId = HttpContext.Session.GetInt32("PreferredLeagueId");
        }

        var leagues = _context.Leagues
            .OrderBy(l => l.Name)
            .Select(l => new LeagueSelectionViewModel
            {
                Id = l.Id,
                Name = l.Name,
                IsSelected = preferredLeagueId.HasValue && l.Id == preferredLeagueId.Value
            })
            .ToList();

        return View(leagues);
    }

    [HttpPost]
    public IActionResult Select(int selectedLeagueId)
    {
        
        var leagueExists = _context.Leagues.Any(l => l.Id == selectedLeagueId);
        if (!leagueExists)
        {
            ModelState.AddModelError(string.Empty, "The selected league is invalid.");
            return View(_context.Leagues
                .OrderBy(l => l.Name)
                .Select(l => new LeagueSelectionViewModel
                {
                    Id = l.Id,
                    Name = l.Name,
                    IsSelected = false
                })
                .ToList());
        }

        if (User.Identity.IsAuthenticated)
        {
        
            var user = _userManager.GetUserAsync(User).Result;
            if (user != null)
            {
                user.FavoriteLeagueId = selectedLeagueId;
                _context.SaveChanges();
            }
        }
        else
        {
           
            HttpContext.Session.SetInt32("PreferredLeagueId", selectedLeagueId);
        }

        return RedirectToAction("Index", "Home");
    }

}
