using Beadando.Data;
using Beadando.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

public class MatchController : Controller
{
    private readonly SportsDbContext _context;

    public MatchController(SportsDbContext context)
    {
        _context = context;
    }

    public IActionResult MatchDetails(int id)
    {
        // Fetch the match details
        var match = _context.Matches
            .Include(m => m.HomeTeam) // Load HomeTeam navigation property
            .Include(m => m.AwayTeam) // Load AwayTeam navigation property
            .FirstOrDefault(m => m.Id == id);

        if (match == null)
        {
            return NotFound();
        }

        // Fetch the last 3 results between the two teams
        var lastResults = _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Where(m =>
                (m.HomeTeamId == match.HomeTeamId && m.AwayTeamId == match.AwayTeamId) ||
                (m.HomeTeamId == match.AwayTeamId && m.AwayTeamId == match.HomeTeamId))
            .OrderByDescending(m => m.StartTime)
            .Take(3)
            .Select(m => new MatchViewModel
            {
                Id = m.Id,
                HomeTeam = m.HomeTeam.Name,
                AwayTeam = m.AwayTeam.Name,
                FinalScore = m.FinalScore,
                StartTime = m.StartTime
            })
            .ToList();

        // Prepare the view model
        var viewModel = new MatchDetailsViewModel
        {
            HomeTeam = match.HomeTeam?.Name ?? "Unknown Home Team",
            AwayTeam = match.AwayTeam?.Name ?? "Unknown Away Team",
            FinalScore = match.FinalScore ?? "N/A",
            HalfTimeScore = match.HalfTimeScore ?? "N/A",
            StartTime = match.StartTime,
            Report = match.Report ?? "No report available",
            LastResults = lastResults
        };

        return View(viewModel);
    }
}
