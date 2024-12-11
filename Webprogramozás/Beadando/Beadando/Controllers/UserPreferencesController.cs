using Beadando.Data;
using Beadando.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Beadando.Controllers
{
    [Authorize(Roles = "Admin")] 
    public class UserPreferencesController : Controller
    {
        private readonly SportsDbContext _context;

        public UserPreferencesController(SportsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            
            var teamPreferences = _context.Users
                .Where(u => u.FavoriteTeamId != null) 
                .GroupBy(u => u.FavoriteTeamId)
                .Select(group => new UserPreferenceViewModel
                {
                    TeamName = _context.Teams.FirstOrDefault(t => t.Id == group.Key).Name ?? "Unknown",
                    PreferenceCount = group.Count()
                })
                .OrderByDescending(up => up.PreferenceCount) 
                .ToList();

            return View(teamPreferences);
        }
    }
}
