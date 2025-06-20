using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Managly.Data;
using Managly.Models;
using System.Security.Claims;

namespace Managly.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("tasks")]
        public async Task<IActionResult> GetTasks()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tasks = await _context.TaskAssignments
                .Include(t => t.Task)
                .Include(t => t.Task.Project)
                .Where(t => t.UserId == userId && t.Task.Status != "Completed")
                .OrderBy(t => t.Task.DueDate)
                .Take(5)
                .Select(t => new {
                    Id = t.Task.Id,
                    Title = t.Task.TaskTitle,
                    Description = t.Task.Description,
                    DueDate = t.Task.DueDate.HasValue ? t.Task.DueDate.Value.ToString("MMM dd, yyyy") : "No due date",
                    Priority = t.Task.Priority,
                    ProjectName = t.Task.Project.Name
                })
                .ToListAsync();

            return Json(tasks);
        }

        [HttpGet("chats")]
        public async Task<IActionResult> GetRecentChats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var recentChats = await _context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderByDescending(m => m.Timestamp)
                .Take(5)
                .Select(m => new {
                    UserId = m.SenderId == userId ? m.ReceiverId : m.SenderId,
                    UserName = m.SenderId == userId
                        ? (m.Receiver.Name + " " + m.Receiver.LastName)
                        : (m.Sender.Name + " " + m.Sender.LastName),
                    ProfilePicture = m.SenderId == userId
                        ? m.Receiver.ProfilePicturePath
                        : m.Sender.ProfilePicturePath,
                    LastMessage = m.Content,
                    Timestamp = m.Timestamp
                })
                .Distinct()
                .ToListAsync();

            return Json(recentChats);
        }

        [HttpGet("timeline")]
        public async Task<IActionResult> GetTimeline()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var projects = await _context.Projects
                .Where(p => p.ProjectMembers.Any(m => m.UserId == userId))
                .Include(p => p.Tasks)
                .OrderBy(p => p.CreatedAt)
                .Take(5)
                .Select(p => new {
                    p.Id,
                    p.Name,
                    Progress = (p.TotalTasks > 0)
                        ? (double)p.CompletedTasks / p.TotalTasks * 100
                        : 0,
                    StartDate = p.CreatedAt.ToString("MMM dd, yyyy"),
                    EndDate = p.Deadline.HasValue ? p.Deadline.Value.ToString("MMM dd, yyyy") : "Ongoing",
                    TaskCount = p.TotalTasks,
                    CompletedTasks = p.CompletedTasks
                })
                .ToListAsync();

            return Json(projects);
        }

        [HttpGet("meetings")]
        public async Task<IActionResult> GetMeetings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.Today;
            var schedules = await _context.Schedules
                .Where(s => s.UserId == userId && s.ShiftDate >= today)
                .OrderBy(s => s.ShiftDate)
                .Take(5)
                .Select(s => new {
                    s.Id,
                    Title = s.Comment ?? "Scheduled Meeting",
                    Date = s.ShiftDate.ToString("MMM dd, yyyy"),
                    Time = s.StartTime.ToString(@"hh\:mm") + " - " + s.EndTime.ToString(@"hh\:mm"),
                    ProjectName = s.Project != null ? s.Project.Name : "General Meeting",
                    Status = s.Status
                })
                .ToListAsync();

            return Json(schedules);
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.Timestamp)
                .Take(5)
                .Select(n => new {
                    n.Id,
                    n.Message,
                    n.Link,
                    Timestamp = n.Timestamp.ToString("MMM dd, yyyy HH:mm")
                })
                .ToListAsync();

            return Json(notifications);
        }

        [HttpPost("layout")]
        public async Task<IActionResult> SaveLayout([FromBody] DashboardLayout layout)
        {
            try
            {
                if (layout == null || string.IsNullOrEmpty(layout.LayoutData))
                {
                    return BadRequest(new { success = false, message = "Layout data is required" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }
                
                var existingLayout = await _context.DashboardLayouts
                    .FirstOrDefaultAsync(l => l.UserId == userId);

                if (existingLayout != null)
                {
                    existingLayout.LayoutData = layout.LayoutData;
                    existingLayout.LastUpdated = DateTime.UtcNow;
                    _context.DashboardLayouts.Update(existingLayout);
                }
                else
                {
                    _context.DashboardLayouts.Add(new DashboardLayout
                    {
                        UserId = userId,
                        LayoutData = layout.LayoutData,
                        LastUpdated = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Layout saved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error saving layout", error = ex.Message });
            }
        }

        [HttpGet("layout")]
        public async Task<IActionResult> GetLayout()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var layout = await _context.DashboardLayouts
                    .FirstOrDefaultAsync(l => l.UserId == userId);

                if (layout == null)
                {
                    return Ok(new { 
                        success = false, 
                        message = "No saved layout found for this user"
                    });
                }

                return Ok(new {
                    success = true,
                    message = "Layout loaded successfully",
                    layoutData = layout.LayoutData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Error loading layout", 
                    error = ex.Message 
                });
            }
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var taskStats = await _context.TaskAssignments
                .Include(t => t.Task)
                .Where(t => t.UserId == userId)
                .GroupBy(t => t.Task.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var projectProgress = await _context.Projects
                .Where(p => p.ProjectMembers.Any(m => m.UserId == userId))
                .Select(p => new {
                    p.Name,
                    Progress = (p.TotalTasks > 0)
                        ? (double)p.CompletedTasks / p.TotalTasks * 100
                        : 0
                })
                .ToListAsync();

            var now = DateTime.Now;
            var activityData = await _context.Attendances
                .Where(a => a.UserId == userId && a.CheckInTime >= now.AddDays(-30))
                .GroupBy(a => a.CheckInTime.Date)
                .Select(g => new {
                    Date = g.Key.ToString("MMM dd"),
                    Count = g.Count(),
                    Hours = g.Sum(a => (a.CheckOutTime.HasValue
                        ? (a.CheckOutTime.Value - a.CheckInTime).TotalHours
                        : (DateTime.Now - a.CheckInTime).TotalHours))
                })
                .ToListAsync();

            return Json(new
            {
                TaskStatistics = taskStats,
                ProjectProgress = projectProgress,
                ActivityTrend = activityData
            });
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReports()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var projectReports = await _context.Projects
                .Where(p => p.ProjectMembers.Any(m => m.UserId == userId))
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new {
                    ProjectName = p.Name,
                    TasksTotal = p.TotalTasks,
                    TasksCompleted = p.CompletedTasks,
                    TasksPending = p.Tasks.Count(t => t.Status == "Pending"),
                    TasksOverdue = p.Tasks.Count(t => t.Status == "Overdue"),
                    TeamSize = p.ProjectMembers.Count,
                    Progress = (p.TotalTasks > 0)
                        ? (double)p.CompletedTasks / p.TotalTasks * 100
                        : 0
                })
                .ToListAsync();

            return Json(projectReports);
        }

        [HttpGet("calendar")]
        public async Task<IActionResult> GetCalendar()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(6);

            var schedules = await _context.Schedules
                .Where(s => s.UserId == userId &&
                           s.ShiftDate >= startOfWeek &&
                           s.ShiftDate <= endOfWeek)
                .OrderBy(s => s.ShiftDate)
                .Select(s => new {
                    id = s.Id,
                    title = $"{s.StartTime.ToString(@"hh\:mm")} - {s.EndTime.ToString(@"hh\:mm")}",
                    start = s.ShiftDate.ToString("yyyy-MM-dd"),
                    description = s.Comment ?? "Work Shift",
                    isToday = s.ShiftDate.Date == today,
                    startTime = s.StartTime.ToString(@"hh\:mm"),
                    endTime = s.EndTime.ToString(@"hh\:mm")
                })
                .ToListAsync();

            var leaves = await _context.Leaves
                .Where(l => l.UserId == userId &&
                           l.LeaveDate >= startOfWeek &&
                           l.LeaveDate <= endOfWeek &&
                           (l.Status == "Approved" || l.Status == "Pending"))
                .Select(l => new {
                    id = l.Id,
                    title = $"Vacation ({l.Status})",
                    start = l.LeaveDate.ToString("yyyy-MM-dd"),
                    description = "Vacation Day",
                    isToday = l.LeaveDate.Date == today,
                    color = l.Status == "Approved" ? "#10B981" : "#F59E0B"
                })
                .ToListAsync();

            return Json(new
            {
                currentWeek = new
                {
                    start = startOfWeek.ToString("yyyy-MM-dd"),
                    end = endOfWeek.ToString("yyyy-MM-dd"),
                    today = today.ToString("yyyy-MM-dd")
                },
                schedules = schedules,
                leaves = leaves
            });
        }

        [HttpGet("clockin")]
        public async Task<IActionResult> GetClockIn()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { success = false, error = "User not found" });

                var currentSession = await _context.Attendances
                    .Where(a => a.UserId == user.Id && a.CheckOutTime == null)
                    .OrderByDescending(a => a.CheckInTime)
                    .FirstOrDefaultAsync();

                if (currentSession == null)
                {
                    return Ok(new
                    {
                        active = false,
                        message = "Not clocked in"
                    });
                }

                return Ok(new
                {
                    active = true,
                    checkInTime = currentSession.CheckInTime,
                    elapsedTime = (DateTime.UtcNow - currentSession.CheckInTime).TotalSeconds,
                    message = "Currently working"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = "An error occurred while checking clock in status",
                    details = ex.Message
                });
            }
        }
    }
}