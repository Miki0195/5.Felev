using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Managly.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Managly.Data;

namespace Managly.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(
            UserManager<User> userManager, 
            ApplicationDbContext context,
            ILogger<ProjectsController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.IsAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            // Get counts of active and archived projects for the ViewBag
            if (currentUser != null)
            {
                var activeCount = await _context.Projects
                    .Where(p => p.CompanyId == currentUser.CompanyId && p.Status != "Completed")
                    .CountAsync();
                    
                var archivedCount = await _context.Projects
                    .Where(p => p.CompanyId == currentUser.CompanyId && p.Status == "Completed")
                    .CountAsync();
                
                ViewBag.ActiveProjectsCount = activeCount;
                ViewBag.ArchivedProjectsCount = archivedCount;
            }

            _logger.LogInformation("Loading Projects Index with partials");

            return View();
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveProjects()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                
                if (currentUser == null)
                {
                    _logger.LogWarning("No current user found");
                    return Unauthorized();
                }

                var projects = await _context.Projects
                    .Where(p => p.CompanyId == currentUser.CompanyId && 
                               p.Status != "Completed")
                    .Select(p => new { id = p.Id, name = p.Name })
                    .ToListAsync();
                
                _logger.LogInformation($"Found {projects.Count} active projects for company {currentUser.CompanyId}");
                return Ok(projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active projects");
                return StatusCode(500, new { error = "Error fetching projects" });
            }
        }
    }
}