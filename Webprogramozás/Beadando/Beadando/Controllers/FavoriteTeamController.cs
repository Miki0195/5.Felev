using Beadando.Data;
using Beadando.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Beadando.Controllers
{
    [Authorize] 
    public class FavoriteTeamController : Controller
    {
        private readonly SportsDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FavoriteTeamController(SportsDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? leagueId)
        {
            var user = await _userManager.GetUserAsync(User);

            
            var leagues = _context.Leagues.OrderBy(l => l.Name).ToList();
            ViewBag.Leagues = leagues;

            
            if (leagueId.HasValue)
            {
                var teams = _context.Teams.Where(t => t.LeagueId == leagueId).ToList();
                ViewBag.Teams = teams;
            }

           
            int? favoriteTeamId = user?.FavoriteTeamId;
            Team favoriteTeam = null;

            if (favoriteTeamId.HasValue)
            {
                favoriteTeam = _context.Teams.FirstOrDefault(t => t.Id == favoriteTeamId);
            }

            ViewBag.FavoriteTeam = favoriteTeam;
            return View();
        }



        [HttpGet]
        public IActionResult TeamsByLeague(int leagueId)
        {
            
            var teams = _context.Teams
                .Where(t => t.LeagueId == leagueId)
                .OrderBy(t => t.Id)
                .ToList();

            if (!teams.Any())
            {
                return Content("<p><i>No teams found for the selected league.</i></p>", "text/html");
            }

            return PartialView("~/Views/FavoriteTeam/_TeamsList.cshtml", teams);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetFavoriteTeam(int teamId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            user.FavoriteTeamId = teamId;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Failed to update favorite team.";
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "Favorite team updated successfully!";
            return RedirectToAction("Index");
        }


    }
}
