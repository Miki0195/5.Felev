using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Beadando.Models;
using Beadando.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Identity;

namespace Beadando.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SportsDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ILogger<HomeController> logger, SportsDbContext context, UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Index(DateTime? date)
    {
        
        DateTime selectedDate = date ?? DateTime.Now.Date;

        
        int? FavoriteLeagueId = null;
        int? FavoriteTeamId = null;

        if (User.Identity.IsAuthenticated)
        {
        
            var user = _userManager.GetUserAsync(User).Result;
            FavoriteLeagueId = user?.FavoriteLeagueId;
            FavoriteTeamId = user?.FavoriteTeamId;
        }
        else
        {
        
            FavoriteLeagueId ??= HttpContext.Session.GetInt32("PreferredLeagueId");
        }

        
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
                        StartTime = m.StartTime,
                        HomeTeamId = m.HomeTeamId ?? 0,
                        AwayTeamId = m.AwayTeamId ?? 0
                    })
                    .ToList()
            })
            .ToList();

        
        var favoriteLeague = _context.Leagues.FirstOrDefault(league => league.Id == FavoriteLeagueId);

        leagues = leagues
            .OrderByDescending(l => favoriteLeague != null && l.LeagueName == favoriteLeague.Name)
            .ThenBy(l => l.LeagueName)
            .ToList();

        
        List<LeagueTableRow> leagueTable = null;
        if (FavoriteLeagueId.HasValue)
        {
            leagueTable = CalculateLeagueTable(FavoriteLeagueId.Value, _context);
        }
        else
        {
        
            var defaultLeague = _context.Leagues.FirstOrDefault();
            if (defaultLeague != null)
            {
                leagueTable = CalculateLeagueTable(defaultLeague.Id, _context);
                favoriteLeague = defaultLeague; 
            }
        }

        
        ViewData["SelectedDate"] = selectedDate;
        ViewData["LeagueTable"] = leagueTable; 
        ViewBag.FavoriteLeagueName = favoriteLeague?.Name;
        ViewBag.FavoriteTeamID = FavoriteTeamId;

        return View(leagues);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    private static List<LeagueTableRow> CalculateLeagueTable(int leagueId, SportsDbContext context)
    {
        
        var matches = context.Matches.Where(m => m.LeagueId == leagueId).ToList();
        var teams = context.Teams.Where(t => t.LeagueId == leagueId).ToList();

        
        return teams.Select(team =>
        {
        
            var teamMatches = matches.Where(m => m.HomeTeamId == team.Id || m.AwayTeamId == team.Id);

        
            int wins = 0, ties = 0, losses = 0;

            foreach (var match in teamMatches)
            {
                int homeScore = GetScore(match.FinalScore, true);
                int awayScore = GetScore(match.FinalScore, false);

                if (match.HomeTeamId == team.Id) 
                {
                    if (homeScore > awayScore) wins++;
                    else if (homeScore == awayScore) ties++;
                    else losses++;
                }
                else if (match.AwayTeamId == team.Id) 
                {
                    if (awayScore > homeScore) wins++;
                    else if (awayScore == homeScore) ties++;
                    else losses++;
                }
            }

            int points = (wins * 3) + (ties * 1);

            return new LeagueTableRow
            {
                TeamName = team.Name,
                MatchesPlayed = teamMatches.Count(),
                Wins = wins,
                Ties = ties,
                Losses = losses,
                Points = points
            };
        })
        .OrderByDescending(row => row.Points)  
        .ThenByDescending(row => row.Wins)    
        .ThenBy(row => row.TeamName)          
        .ToList();
    }

    private static int GetScore(string score, bool isHome)
    {
        if (string.IsNullOrWhiteSpace(score)) return 0; 
        var parts = score.Split(':');                 
        return int.TryParse(parts[isHome ? 0 : 1], out var result) ? result : 0;
    }


    private int? GetFavoriteLeagueIdForUser(string userId)
    {
        var preference = _context.UserPreferences.FirstOrDefault(p => p.UserId == userId);
        return preference?.FavoriteLeagueId;
    }

}

