using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Managly.Models;

namespace Managly.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly UserManager<User> _userManager;

        public ProjectsController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.IsAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            return View();
        }
    }
}