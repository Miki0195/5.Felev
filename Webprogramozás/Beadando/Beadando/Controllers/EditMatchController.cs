using System.Security.Claims;
using Beadando.Data;
using Beadando.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beadando.Controllers
{
    [Authorize(Roles = "Admin")] 
    public class EditMatchController : Controller
    {
        private readonly SportsDbContext _context;

        public EditMatchController(SportsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Teams = _context.Teams.OrderBy(t => t.Name).ToList();
            ViewBag.Leagues = _context.Leagues.OrderBy(l => l.Name).ToList();
            return View("~/Views/Match/Create.cshtml");
        }

        [HttpPost]
        public IActionResult Create(Match match)
        {
            if (ModelState.IsValid)
            {
                var homeTeam = _context.Teams.FirstOrDefault(t => t.Id == match.HomeTeamId);
                var awayTeam = _context.Teams.FirstOrDefault(t => t.Id == match.AwayTeamId);
                var league = _context.Leagues.FirstOrDefault(l => l.Id == match.LeagueId);

                if (homeTeam == null || awayTeam == null || league == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid team or league selection.");
                    ViewBag.Teams = _context.Teams.OrderBy(t => t.Name).ToList();
                    ViewBag.Leagues = _context.Leagues.OrderBy(l => l.Name).ToList();
                    return View("~/Views/Match/Create.cshtml");
                }

                match.Id = (_context.Matches.Max(m => (int?)m.Id) ?? 0) + 1;
                _context.Matches.Add(match);
                _context.SaveChanges();

                TempData["CreateSuccessMessage"] = "Match added successfully!";
                ViewData["FormSubmitted"] = true;
                ModelState.Clear();

                ViewBag.Teams = _context.Teams.OrderBy(t => t.Name).ToList();
                ViewBag.Leagues = _context.Leagues.OrderBy(l => l.Name).ToList();
                return View("~/Views/Match/Create.cshtml");
            }

            Console.WriteLine("ModelState is invalid:");
            foreach (var entry in ModelState)
            {
                foreach (var error in entry.Value.Errors)
                {
                    Console.WriteLine($"Error in {entry.Key}: {error.ErrorMessage}");
                }
            }

            ViewBag.Teams = _context.Teams.OrderBy(t => t.Name).ToList();
            ViewBag.Leagues = _context.Leagues.OrderBy(l => l.Name).ToList();
            return View("~/Views/Match/Create.cshtml");
        }

        [Authorize(Roles = "Admin")] 
        [HttpGet]
        public IActionResult SearchMatches(string teamName)
        {
            if (string.IsNullOrWhiteSpace(teamName))
            {
                TempData["NoGivenTeam"] = "Please enter a team!";
                return View("~/Views/Match/SearchMatches.cshtml");
            }

            var matches = _context.Matches
                .Where(m =>
                    m.HomeTeam.Name.ToLower().Contains(teamName.ToLower()) ||
                    m.AwayTeam.Name.ToLower().Contains(teamName.ToLower()))
                .OrderByDescending(m => m.StartTime)
                .Select(m => new MatchViewModel
                {
                    Id = m.Id,
                    HomeTeam = m.HomeTeam.Name,
                    AwayTeam = m.AwayTeam.Name,
                    FinalScore = m.FinalScore,
                    StartTime = m.StartTime
                })
                .ToList();

            if (!matches.Any())
            {
                TempData["NoValidTeam"] = "No matches found for the given team.";
            }

            return View("~/Views/Match/SearchMatches.cshtml", matches);
        }

        [Authorize(Roles = "Admin")] 
        [HttpPost]
        public IActionResult DeleteMatch(int id)
        {
            var match = _context.Matches.FirstOrDefault(m => m.Id == id);
            if (match == null)
            {
                return NotFound();
            }

            _context.Matches.Remove(match);
            _context.SaveChanges();

            TempData["DeleteSuccessMessage"] = "Match deleted successfully!";
            return RedirectToAction("SearchMatches");
        }


        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult SearchMatchesForEdit(string teamName)
        {
            if (string.IsNullOrWhiteSpace(teamName))
            {
                TempData["NoGivenTeam"] = "Please enter a team name.";
                return View("~/Views/Match/SearchMatchesForEdit.cshtml");
            }

            HttpContext.Session.SetString("TeamName", teamName);

            var matches = _context.Matches
                .Where(m =>
                    m.HomeTeam.Name.ToLower().Contains(teamName.ToLower()) ||
                    m.AwayTeam.Name.ToLower().Contains(teamName.ToLower()))
                .OrderByDescending(m => m.StartTime)
                .Select(m => new MatchViewModel
                {
                    Id = m.Id,
                    HomeTeam = m.HomeTeam.Name, 
                    AwayTeam = m.AwayTeam.Name, 
                    FinalScore = m.FinalScore,
                    HalfTimeScore = m.HalfTimeScore,
                    StartTime = m.StartTime,
                    Report = m.Report
                })
                .ToList();

            if (!matches.Any())
            {
                TempData["NoValidTeam"] = "No matches found for the given team.";
            }

            TempData["TeamName"] = teamName;
            ViewBag.TeamName = teamName;

            return View("~/Views/Match/SearchMatchesForEdit.cshtml", matches);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult EditMatch(int id)
        {
            var match = _context.Matches.FirstOrDefault(m => m.Id == id);
            if (match == null)
            {
                return NotFound();
            }

            
            ViewBag.Teams = _context.Teams.OrderBy(t => t.Name).ToList();
            ViewBag.Leagues = _context.Leagues.OrderBy(l => l.Name).ToList();

            return PartialView("~/Views/Match/_EditMatchModal.cshtml", match);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditMatch(Match model, string teamName)
        {
            if (!ModelState.IsValid)
            {
               
                ViewBag.Teams = _context.Teams.OrderBy(t => t.Name).ToList();
                ViewBag.Leagues = _context.Leagues.OrderBy(l => l.Name).ToList();
                return PartialView("~/Views/Match/_EditMatchModal.cshtml", model);
            }

            var match = _context.Matches.FirstOrDefault(m => m.Id == model.Id);
            if (match == null)
            {
                return NotFound();
            }

            
            match.HomeTeamId = model.HomeTeamId.Value;
            match.AwayTeamId = model.AwayTeamId.Value;
            match.LeagueId = model.LeagueId.Value;
            match.StartTime = model.StartTime;
            match.FinalScore = model.FinalScore;
            match.HalfTimeScore = model.HalfTimeScore;
            match.Report = model.Report;

            _context.SaveChanges();

            TempData["EditSuccessMessage"] = "Match updated successfully!";

            var matches = _context.Matches
            .Where(m =>
                m.HomeTeam.Name.ToLower().Contains(teamName.ToLower()) ||
                m.AwayTeam.Name.ToLower().Contains(teamName.ToLower()))
            .OrderByDescending(m => m.StartTime)
            .Select(m => new MatchViewModel
            {
                Id = m.Id,
                HomeTeam = m.HomeTeam.Name, 
                AwayTeam = m.AwayTeam.Name, 
                FinalScore = m.FinalScore,
                HalfTimeScore = m.HalfTimeScore,
                StartTime = m.StartTime,
                Report = m.Report
            })
            .ToList();

            return View("~/Views/Match/SearchMatchesForEdit.cshtml");
        }

        [HttpGet]
        public IActionResult GetTeamsByLeague(int leagueId)
        {
            var teams = _context.Teams
                .Where(t => t.LeagueId == leagueId)
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name })
                .ToList();

            return Json(teams);
        }
    }
}
