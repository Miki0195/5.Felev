using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Beadando.Models;
using Beadando.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Beadando.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SportsDbContext _context;

    public HomeController(ILogger<HomeController> logger, SportsDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index(DateTime? date)
    {
        // Default to current day if no date is provided
        DateTime selectedDate = date ?? DateTime.Now.Date;

        // Get the preferred league ID from the session
        int? preferredLeagueId = HttpContext.Session.GetInt32("PreferredLeagueId");

        // Fetch leagues and their matches for the selected date
        var leagues = _context.Leagues
            .Select(league => new LeagueMatchesViewModel
            {
                LeagueName = league.Name,
                Matches = _context.Matches
                    .Where(m => m.LeagueId == league.Id && m.StartTime.Date == selectedDate)
                    .OrderBy(m => m.StartTime)
                    .Select(m => new MatchViewModel
                    {
                        Id = m.Id,
                        HomeTeam = _context.Teams.FirstOrDefault(t => t.Id == m.HomeTeamId).Name,
                        AwayTeam = _context.Teams.FirstOrDefault(t => t.Id == m.AwayTeamId).Name,
                        FinalScore = m.FinalScore,
                        StartTime = m.StartTime
                    })
                    .ToList()
            })
            .ToList();

        // Reorder leagues: preferred league first, others in alphabetical order
        if (preferredLeagueId.HasValue)
        {
            leagues = leagues
                .OrderByDescending(l => l.LeagueName == _context.Leagues.FirstOrDefault(league => league.Id == preferredLeagueId).Name)
                .ThenBy(l => l.LeagueName)
                .ToList();
        }

        // Pass the selected date and leagues to the view
        ViewData["SelectedDate"] = selectedDate;
        return View(leagues);
    }


    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult TestDatabase()
    {

        var leagues = _context.Leagues.ToList();
        var teams = _context.Teams.ToList();
        var matches = _context.Matches.ToList();

        if (!leagues.Any() && !teams.Any() && !matches.Any())
        {
            return Content("Database exists but all tables are empty.");
        }

        return Content($"Leagues: {leagues.Count}, Teams: {teams.Count}, Matches: {matches.Count}");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

