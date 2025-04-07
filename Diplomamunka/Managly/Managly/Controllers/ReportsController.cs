using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Managly.Data;
using Managly.Models;

namespace Managly.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ReportsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("api/reports/projects")]
        public async Task<IActionResult> GetProjects()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var projects = await _context.Projects
                .Where(p => p.CompanyId == currentUser.CompanyId)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();
            return Ok(projects);
        }

        [HttpGet("api/reports/project-metrics")]
        public async Task<IActionResult> GetProjectMetrics(
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    [FromQuery] int? projectId,
    [FromQuery] string? status,
    [FromQuery] string? priority,
    [FromQuery] string? teamMemberId
)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    Console.WriteLine("❌ ERROR: Current user not found");
                    return StatusCode(500, new { error = "Current user not found" });
                }

                var query = _context.Projects
                    .Include(p => p.Tasks)
                    .Include(p => p.ProjectMembers)
                    .Where(p => p.CompanyId == currentUser.CompanyId);

                // Apply date filters
                if (startDate.HasValue)
                    query = query.Where(p => p.CreatedAt >= startDate);
                if (endDate.HasValue)
                    query = query.Where(p => p.CreatedAt <= endDate);

                // Apply project ID filter
                if (projectId.HasValue)
                    query = query.Where(p => p.Id == projectId);

                // ✅ Apply single Status filter (Ensure it does not break if empty)
                if (!string.IsNullOrEmpty(status) && status != "all" && status != "none")
                    query = query.Where(p => p.Status == status);

                // ✅ Apply single Priority filter (Ensure it does not break if empty)
                if (!string.IsNullOrEmpty(priority) && priority != "all" && priority != "none")
                    query = query.Where(p => p.Priority == priority);

                // ✅ Apply single Team Member filter (Ensure it does not break if empty)
                if (!string.IsNullOrEmpty(teamMemberId) && teamMemberId != "all" && teamMemberId != "none")
                    query = query.Where(p => p.ProjectMembers.Any(m => m.UserId == teamMemberId));

                var metrics = await query
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Status,
                        p.Priority,
                        Progress = p.TotalTasks == 0 ? 0 : (p.CompletedTasks * 100 / p.TotalTasks),
                        TasksCount = p.TotalTasks,
                        CompletedTasks = p.CompletedTasks,
                        OverdueTasks = p.Tasks.Count(t => t.DueDate != null && t.DueDate < DateTime.UtcNow && t.Status != "Completed"),
                        TeamSize = p.ProjectMembers.Count
                    })
                    .ToListAsync();

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR in GetProjectMetrics: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new
                {
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message,
                    innerStackTrace = ex.InnerException?.StackTrace
                });
            }
        }






        [HttpGet("api/reports/user-productivity")]
        public async Task<IActionResult> GetUserProductivity(
            [FromQuery] DateTime? startDate, 
            [FromQuery] DateTime? endDate,
            [FromQuery] int? projectId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return StatusCode(500, new { error = "Current user not found" });
                }

                // Get users based on project or company
                var usersQuery = projectId.HasValue
                    ? from pm in _context.ProjectMembers
                      join u in _context.Users on pm.UserId equals u.Id
                      where pm.ProjectId == projectId
                      select u
                    : _context.Users.Where(u => u.CompanyId == currentUser.CompanyId);

                var users = await usersQuery.ToListAsync();

                // Get task assignments and tasks in one query
                var taskData = await (from ta in _context.TaskAssignments
                                    join t in _context.Tasks on ta.TaskId equals t.Id
                                    where users.Select(u => u.Id).Contains(ta.UserId) &&
                                          (!projectId.HasValue || t.ProjectId == projectId)
                                    group ta by ta.UserId into g
                                    select new
                                    {
                                        UserId = g.Key,
                                        TasksAssigned = g.Count(),
                                        TasksCompleted = g.Count(x => x.Task.Status == "Completed")
                                    }).ToDictionaryAsync(x => x.UserId);

                // Get attendance data
                var rawAttendanceData = await _context.Attendances
                    .Where(a => users.Select(u => u.Id).Contains(a.UserId) &&
                                (!startDate.HasValue || a.CheckInTime >= startDate) &&
                                (!endDate.HasValue || a.CheckInTime <= endDate))
                    .ToListAsync();  // Fetch raw data first

                // Compute Total Hours in C#
                var attendanceData = rawAttendanceData
                    .GroupBy(a => a.UserId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(a =>
                            ((a.CheckOutTime ?? DateTime.UtcNow) - a.CheckInTime).TotalHours  // Perform subtraction in C#
                        )
                    );




                var result = users.Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.LastName,
                    u.ProfilePicturePath,
                    TasksAssigned = taskData.ContainsKey(u.Id) ? taskData[u.Id].TasksAssigned : 0,
                    TasksCompleted = taskData.ContainsKey(u.Id) ? taskData[u.Id].TasksCompleted : 0,
                    TotalWorkingHours = attendanceData.TryGetValue(u.Id, out var hours) ? hours : 0  // Safe dictionary lookup
                }).ToList();


                // Log data for debugging
                foreach (var item in result)
                {
                    Console.WriteLine($"User {item.Name}: Tasks={item.TasksAssigned}/{item.TasksCompleted}, Hours={item.TotalWorkingHours}");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                var error = new
                {
                    message = ex.Message,
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message,
                    innerStackTrace = ex.InnerException?.StackTrace
                };
                Console.WriteLine($"Error in GetUserProductivity: {Newtonsoft.Json.JsonConvert.SerializeObject(error)}");
                return StatusCode(500, error);
            }
        }

        [HttpGet("api/reports/task-distribution")]
        public async Task<IActionResult> GetTaskDistribution([FromQuery] int? projectId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var query = _context.Tasks
                    .Include(t => t.Project)
                    .Where(t => t.Project.CompanyId == currentUser.CompanyId);

                if (projectId.HasValue)
                    query = query.Where(t => t.ProjectId == projectId);

                var distribution = await query
                    .GroupBy(t => t.Status ?? "Unknown")
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                return Ok(distribution);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("api/reports/task-distribution-by-priority")]
        public async Task<IActionResult> GetTaskDistributionByPriority([FromQuery] int? projectId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var query = _context.Tasks
                    .Include(t => t.Project)
                    .Where(t => t.Project.CompanyId == currentUser.CompanyId);

                if (projectId.HasValue)
                    query = query.Where(t => t.ProjectId == projectId);

                var distribution = await query
                    .GroupBy(t => t.Priority ?? "Unknown")
                    .Select(g => new { Priority = g.Key, Count = g.Count() })
                    .ToListAsync();

                return Ok(distribution);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}