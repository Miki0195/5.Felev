using Microsoft.AspNetCore.Mvc;
using Beadando.Data; 
using Beadando.Models; 
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Beadando.Controllers;

public class SearchController : Controller
{
    private readonly SportsDbContext _context;

    public SearchController(SportsDbContext context)
    {
        _context = context;
    }

    public IActionResult Matches(string teamName)
    {
        if (string.IsNullOrEmpty(teamName))
        {
            return View("SearchResults", null); 
        }

        teamName = teamName.ToLower();

        var team = _context.Teams.FirstOrDefault(t => t.Name.ToLower() == teamName);
        if (team == null)
        {
            return View("SearchResults", null); 
        }

        int teamId = team.Id;

        var matches = _context.Matches
            .Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId)
            .Select(m => new MatchViewModel
            {
                HomeTeam = _context.Teams.FirstOrDefault(t => t.Id == m.HomeTeamId).Name,
                AwayTeam = _context.Teams.FirstOrDefault(t => t.Id == m.AwayTeamId).Name,
                FinalScore = m.FinalScore,
                HalfTimeScore = m.HalfTimeScore,
                StartTime = m.StartTime,
                League = _context.Leagues.FirstOrDefault(l => l.Id == m.LeagueId).Name,
                Report = m.Report
            })
            .OrderByDescending(m => m.StartTime)
            .ToList();

        int wins = matches.Count(m =>
            (m.HomeTeam == team.Name && GetScore(m.FinalScore, true) > GetScore(m.FinalScore, false)) ||
            (m.AwayTeam == team.Name && GetScore(m.FinalScore, false) > GetScore(m.FinalScore, true))    
        );

        int losses = matches.Count(m =>
            (m.HomeTeam == team.Name && GetScore(m.FinalScore, true) < GetScore(m.FinalScore, false)) || 
            (m.AwayTeam == team.Name && GetScore(m.FinalScore, false) < GetScore(m.FinalScore, true))    
        );

        int draws = matches.Count(m =>
            GetScore(m.FinalScore, true) == GetScore(m.FinalScore, false) 
        );

        var viewModel = new TeamPerformanceViewModel
        {
            TeamName = team.Name,
            Wins = wins,
            Losses = losses,
            Draws = draws,
            Matches = matches
        };

        return View("SearchResults", viewModel); 
    }


    private int GetScore(string finalScore, bool isHome)
    {
        if (string.IsNullOrWhiteSpace(finalScore))
            return 0; 

        var scores = finalScore.Split(':');
        if (scores.Length != 2)
            return 0; 

        if (!int.TryParse(scores[0].Trim(), out int homeScore) || !int.TryParse(scores[1].Trim(), out int awayScore))
            return 0; 

        return isHome ? homeScore : awayScore;
    }

    [HttpGet]
    public IActionResult GetTeamSuggestions(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Json(new List<string>()); 
        }

        var suggestions = _context.Teams
            .Where(t => t.Name.ToLower().StartsWith(term.ToLower()))
            .Select(t => t.Name)
            .Take(5) 
            .ToList();

        return Json(suggestions);
    }
}
