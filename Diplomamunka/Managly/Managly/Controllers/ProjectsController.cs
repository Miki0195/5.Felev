using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Managly.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Managly.Data;
using Org.BouncyCastle.Asn1.Ocsp;
using SendGrid.Helpers.Mail;
using System.Text.Json;

namespace Managly.Controllers
{
    [Authorize]
    [Route("[controller]")]
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

        // ============= OWN CODE
        [HttpGet("sidebarPartial")]
        public async Task<IActionResult> GetSidebarPartial([FromQuery] string filter = "all", [FromQuery] string search = "")
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Unauthorized();
                }

                // Base query to get projects for the current user's company
                var query = _context.Projects
                    .Where(p => p.CompanyId == currentUser.CompanyId);

                // Apply filter
                if (filter != "all")
                {
                    switch (filter.ToLower())
                    {
                        case "in progress":
                            query = query.Where(p => p.Status == "In Progress");
                            break;
                        case "not started":
                            query = query.Where(p => p.Status == "Not Started");
                            break;
                        case "completed":
                            query = query.Where(p => p.Status == "Completed");
                            break;
                            // Add other status filters as needed
                    }
                }

                // Apply search
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()));
                }

                // Order projects
                var projects = await query
                    .OrderBy(p => p.Status == "In Progress" ? 0 :
                                  p.Status == "Not Started" ? 1 :
                                  p.Status == "Completed" ? 2 : 3)
                    .ToListAsync();

                _logger.LogInformation($"Found {projects.Count} projects for sidebar with filter={filter}, search={search}");

                // Pass the projects to a partial view
                return PartialView("_ProjectsList", projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering projects sidebar partial");
                return StatusCode(500, $"Error rendering sidebar: {ex.Message}");
            }
        }

        [HttpGet("createModal")]
        public IActionResult GetCreateProjectModal()
        {
            // Return the partial view
            return PartialView("_CreateProjectModal");
        }

        [HttpGet("{id}")]
        [HttpGet("Details")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                _logger.LogInformation($"Loading project details for project ID: {id}");

                var project = await _context.Projects
                    .Include(p => p.ProjectMembers)
                        .ThenInclude(m => m.User)
                    .Include(p => p.CreatedBy)
                    .Include(p => p.Tasks)
                        .ThenInclude(t => t.Assignments)
                            .ThenInclude(a => a.User)
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == currentUser.CompanyId);

                if (project == null)
                {
                    _logger.LogWarning($"Project with ID {id} not found or user {currentUser.Id} doesn't have access");
                    return NotFound();
                }

                _logger.LogInformation($"Project {project.Name} found. Status: {project.Status}");

                // Check if project is archived
                if (project.Status == "Completed")
                {
                    return RedirectToAction("ArchivedDetails", new { id = project.Id });
                }

                // Check if current user is project lead
                var isProjectLead = project.ProjectMembers
                    .Any(m => m.UserId == currentUser.Id && m.Role == "Project Lead");

                // Calculate progress percentage
                int progressPercentage = project.TotalTasks > 0
                    ? (int)Math.Round((double)project.CompletedTasks / project.TotalTasks * 100)
                    : 0;

                // Get recent activities
                var activities = await _context.ActivityLogs
                    .Include(a => a.User)
                    .Where(a => a.ProjectId == id)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(15)
                    .ToListAsync();

                // Create the view model
                var viewModel = new ProjectDetailsViewModel
                {
                    Id = project.Id,
                    Name = project.Name,
                    Description = project.Description ?? string.Empty,
                    FormattedStartDate = project.StartDate.ToString("MMM dd, yyyy"),
                    FormattedDeadline = project.Deadline?.ToString("MMM dd, yyyy") ?? "No deadline set",
                    Status = project.Status ?? "Unknown",
                    StatusCssClass = GetStatusCssClass(project.Status),
                    Priority = project.Priority ?? "Medium",
                    PriorityCssClass = GetPriorityCssClass(project.Priority),
                    IsProjectLead = isProjectLead,
                    TotalTasks = project.TotalTasks,
                    CompletedTasks = project.CompletedTasks,
                    ProgressPercentage = progressPercentage,
                    CurrentUserId = currentUser.Id,

                    TeamMembers = project.ProjectMembers?.Select(m => new ProjectMemberViewModel
                    {
                        UserId = m.User?.Id ?? string.Empty,
                        Name = m.User?.Name ?? string.Empty,
                        LastName = m.User?.LastName ?? string.Empty,
                        Role = m.Role ?? "Member",
                        ProfilePicturePath = m.User?.ProfilePicturePath ?? "/images/default/default-profile.png"
                    }).ToList() ?? new List<ProjectMemberViewModel>(),

                    Tasks = project.Tasks?.Select(t => new TaskViewModel
                    {
                        Id = t.Id,
                        Title = t.TaskTitle ?? "Untitled Task",
                        Description = t.Description ?? "No description",
                        FormattedDueDate = t.DueDate?.ToString("MMM dd, yyyy") ?? "No due date",
                        DueDate = (DateTime)t.DueDate,
                        IsOverdue = t.DueDate.HasValue && t.DueDate.Value < DateTime.Today && t.Status != "Completed",
                        Priority = t.Priority ?? "Medium",
                        PriorityCssClass = GetPriorityCssClass(t.Priority),
                        Status = t.Status ?? "Not Started",
                        StatusCssClass = GetStatusCssClass(t.Status),
                        AssignedUsers = t.Assignments?.Select(a => new AssignedUserViewModel
                        {
                            Id = a.User?.Id ?? string.Empty,
                            Name = a.User?.Name ?? string.Empty,
                            LastName = a.User?.LastName ?? string.Empty,
                            ProfilePicturePath = a.User?.ProfilePicturePath ?? "/images/default/default-profile.png"
                        }).ToList() ?? new List<AssignedUserViewModel>()
                    }).ToList() ?? new List<TaskViewModel>(),

                    Activities = activities.Select(a => new ActivityViewModel
                    {
                        Type = a.Type ?? string.Empty,
                        UserId = a.UserId ?? string.Empty,
                        UserName = a.User != null ? $"{a.User.Name} {a.User.LastName}" : "Unknown User",
                        UserAvatar = a.User?.ProfilePicturePath ?? "/images/default/default-profile.png",
                        TimeAgo = GetTimeAgo(a.Timestamp),
                        Description = a.Action ?? string.Empty,
                        TargetType = a.TargetType ?? string.Empty,
                        TargetId = a.TargetId ?? string.Empty,
                        TargetName = a.TargetName ?? string.Empty
                    }).ToList()
                };

                ViewBag.CurrentUserId = currentUser.Id;
                ViewBag.ProjectId = id;

                _logger.LogInformation($"Successfully built view model for project {project.Name}");

                // Return partial view for AJAX requests, full view otherwise
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_ProjectDetails", viewModel);
                }

                return View("ProjectDetails", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading project with ID {id}");
                return StatusCode(500, $"An error occurred while loading the project: {ex.Message}");
            }
        }
        

        private string GetTimeAgo(DateTime timestamp)
        {
            var difference = DateTime.UtcNow - timestamp;

            if (difference.TotalSeconds < 60)
                return "just now";
            if (difference.TotalMinutes < 60)
                return $"{(int)difference.TotalMinutes} minutes ago";
            if (difference.TotalHours < 24)
                return $"{(int)difference.TotalHours} hours ago";
            if (difference.TotalDays < 7)
                return $"{(int)difference.TotalDays} days ago";

            return timestamp.ToString("MMM dd, yyyy");
        }

        // API endpoint for task filtering (to be used by client-side code)
        [HttpGet("api/tasks/{projectId}")]
        public async Task<IActionResult> GetFilteredTasks(int projectId, string filter = "all")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var query = _context.Tasks
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.User)
                .Where(t => t.ProjectId == projectId);

            // Apply filter
            switch (filter.ToLower())
            {
                case "my":
                    query = query.Where(t => t.Assignments.Any(a => a.UserId == currentUser.Id));
                    break;
                case "pending":
                    query = query.Where(t => t.Status != "Completed");
                    break;
                case "completed":
                    query = query.Where(t => t.Status == "Completed");
                    break;
            }

            var tasks = await query.ToListAsync();

            var taskViewModels = tasks.Select(t => new TaskViewModel
            {
                Id = t.Id,
                Title = t.TaskTitle,
                Description = t.Description ?? "No description",
                FormattedDueDate = t.DueDate?.ToString("MMM dd, yyyy") ?? "No due date",
                DueDate = (DateTime)t.DueDate,
                IsOverdue = t.DueDate.HasValue && t.DueDate.Value < DateTime.Today && t.Status != "Completed", // Check if overdue
                Priority = t.Priority,
                PriorityCssClass = GetPriorityCssClass(t.Priority),
                Status = t.Status,
                StatusCssClass = GetStatusCssClass(t.Status),
                AssignedUsers = t.Assignments.Select(a => new AssignedUserViewModel
                {
                    Id = a.User.Id,
                    Name = a.User.Name,
                    LastName = a.User.LastName,
                    ProfilePicturePath = a.User.ProfilePicturePath ?? "/images/default/default-profile.png"
                }).ToList()
            }).ToList();

            ViewBag.CurrentUserId = currentUser.Id; // This is crucial for the isAssignedToMe check
            ViewBag.ProjectId = projectId;

            return PartialView("_TasksList", taskViewModels);
        }

        [HttpGet("{id}/ActivityFeed")]
        public async Task<IActionResult> GetActivityFeed(int id)
        {
            try
            {
                _logger.LogInformation($"Activity feed requested for project ID: {id}");
                
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("Activity feed: No current user found");
                    return Unauthorized();
                }

                // Check if project exists and user has access
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == currentUser.CompanyId);

                if (project == null)
                {
                    _logger.LogWarning($"Activity feed: Project with ID {id} not found or user {currentUser.Id} doesn't have access");
                    return NotFound();
                }

                // Get recent activities - with error handling for each step
                try
                {
                    _logger.LogDebug($"Fetching activities for project {id}");
                    
                    var activityQuery = _context.ActivityLogs
                        .Include(a => a.User)
                        .Where(a => a.ProjectId == id)
                        .OrderByDescending(a => a.Timestamp)
                        .Take(15);
                        
                    _logger.LogDebug("Activity query constructed");
                    
                    var activityList = await activityQuery.ToListAsync();
                    _logger.LogDebug($"Retrieved {activityList.Count} activities");
                    
                    var viewModels = activityList.Select(a => new ActivityViewModel
                    {
                        Type = a.Type,
                        UserId = a.UserId,
                        UserName = a.User != null ? $"{a.User.Name} {a.User.LastName}" : "Unknown User",
                        UserAvatar = a.User?.ProfilePicturePath ?? "/images/default/default-profile.png",
                        TimeAgo = GetTimeAgo(a.Timestamp),
                        Description = a.Action,
                        TargetType = a.TargetType,
                        TargetId = a.TargetId,
                        TargetName = a.TargetName
                    }).ToList();
                    
                    _logger.LogInformation($"Successfully prepared {viewModels.Count} activity view models for project {id}");
                    
                    return PartialView("_ActivityFeed", viewModels);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing activities for project {id}");
                    
                    // Return an empty list rather than error - this is more user-friendly
                    return PartialView("_ActivityFeed", new List<ActivityViewModel>());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unhandled exception in GetActivityFeed for project {id}");
                return StatusCode(500, $"An error occurred while loading activities: {ex.Message}");
            }
        }

        [HttpGet("archived/{id}")]
        public async Task<IActionResult> ArchivedDetails(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                _logger.LogInformation($"Loading archived project details for project ID: {id}");

                var project = await _context.Projects
                    .Include(p => p.ProjectMembers)
                        .ThenInclude(m => m.User)
                    .Include(p => p.CreatedBy)
                    .Include(p => p.Tasks)
                        .ThenInclude(t => t.Assignments)
                            .ThenInclude(a => a.User)
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == currentUser.CompanyId);

                if (project == null)
                {
                    _logger.LogWarning($"Project with ID {id} not found or user {currentUser.Id} doesn't have access");
                    return NotFound();
                }

                // Create the view model for archived project
                var viewModel = new ArchivedProjectViewModel
                {
                    Id = project.Id,
                    Name = project.Name ?? "Untitled Project",
                    Description = project.Description ?? string.Empty,
                    FormattedStartDate = project.StartDate.ToString("MMM dd, yyyy"),
                    FormattedDeadline = project.Deadline?.ToString("MMM dd, yyyy") ?? "No deadline set",
                    FormattedCompletionDate = project.CompletedAt?.ToString("MMM dd, yyyy") ?? project.UpdatedAt?.ToString("MMM dd, yyyy") ?? "Unknown",
                    Priority = project.Priority ?? "Medium",
                    PriorityCssClass = GetPriorityCssClass(project.Priority),
                    CreatorName = project.CreatedBy != null ? $"{project.CreatedBy.Name} {project.CreatedBy.LastName}" : 
                        (project.ProjectMembers?.FirstOrDefault(m => m.Role == "Project Lead")?.User != null ? 
                         $"{project.ProjectMembers.First(m => m.Role == "Project Lead").User.Name} {project.ProjectMembers.First(m => m.Role == "Project Lead").User.LastName}" : 
                         "Unknown"),

                    TeamMembers = project.ProjectMembers?.Select(m => new ProjectMemberViewModel
                    {
                        UserId = m.User?.Id ?? string.Empty,
                        Name = m.User?.Name ?? string.Empty,
                        LastName = m.User?.LastName ?? string.Empty,
                        Role = m.Role ?? "Member",
                        ProfilePicturePath = m.User?.ProfilePicturePath ?? "/images/default/default-profile.png"
                    }).ToList() ?? new List<ProjectMemberViewModel>(),

                    Tasks = project.Tasks?.Select(t => new TaskViewModel
                    {
                        Id = t.Id,
                        Title = t.TaskTitle ?? "Untitled Task",
                        Description = t.Description ?? "No description",
                        FormattedDueDate = t.DueDate?.ToString("MMM dd, yyyy") ?? "No due date",
                        Priority = t.Priority ?? "Medium",
                        PriorityCssClass = GetPriorityCssClass(t.Priority),
                        Status = t.Status ?? "Not Started",
                        StatusCssClass = GetStatusCssClass(t.Status),
                        AssignedUsers = t.Assignments?.Select(a => new AssignedUserViewModel
                        {
                            Id = a.User?.Id ?? string.Empty,
                            Name = a.User?.Name ?? string.Empty,
                            LastName = a.User?.LastName ?? string.Empty,
                            ProfilePicturePath = a.User?.ProfilePicturePath ?? "/images/default/default-profile.png"
                        }).ToList() ?? new List<AssignedUserViewModel>()
                    }).ToList() ?? new List<TaskViewModel>()
                };

                _logger.LogInformation($"Successfully built view model for archived project {project.Name}");

                // Return partial view for AJAX requests, full view otherwise
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_ArchivedProjectDetails", viewModel);
                }

                return View("ArchivedProjectDetails", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading archived project with ID {id}");
                return StatusCode(500, $"An error occurred while loading the archived project: {ex.Message}");
            }
        }

        private string GetStatusCssClass(string status)
        {
            return status?.ToLower() switch
            {
                "completed" => "status-inprogress",
                "in progress" => "status-inprogress",
                "not started" => "status-notstarted",
                _ => "secondary"
            };
        }

        private string GetPriorityCssClass(string priority)
        {
            return priority?.ToLower() switch
            {
                "high" => "priority-high",
                "medium" => "priority-medium",
                "low" => "priority-low",
                _ => "secondary"
            };
        }

        [HttpPost("restore/{id}")]
        public async Task<IActionResult> RestoreProject(int id, [FromBody] RestoreProjectModel model)
        {
            try
            {
                _logger.LogInformation($"Attempting to restore project {id} with status: {model?.Status}, priority: {model?.Priority}, deadline: {model?.Deadline}");
                
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning($"Restore project {id}: User not authenticated");
                    return Unauthorized();
                }
                
                // Check if user is admin or has appropriate permissions
                if (!await _userManager.IsInRoleAsync(currentUser, "Admin"))
                {
                    _logger.LogWarning($"Restore project {id}: User {currentUser.Id} is not an admin");
                    return Forbid();
                }

                // Find the project
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == currentUser.CompanyId);

                if (project == null)
                {
                    _logger.LogWarning($"Restore project {id}: Project not found or access denied");
                    return NotFound();
                }

                _logger.LogInformation($"Restoring project {id} '{project.Name}' from status '{project.Status}' to '{model.Status}'");

                // Update project properties
                project.Status = model.Status;
                project.Priority = model.Priority;
                project.Deadline = model.Deadline;
                project.CompletedAt = null; // Clear completion date
                project.UpdatedAt = DateTime.UtcNow;

                // Create additional data JSON for the activity log
                string additionalDataJson = "{}"; // Default empty JSON object
                try
                {
                    additionalDataJson = JsonSerializer.Serialize(new { 
                        PreviousStatus = "Completed", 
                        NewStatus = model.Status,
                        NewDeadline = model.Deadline,
                        RestoredAt = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to create additional data JSON: {ex.Message}");
                    // Continue with empty JSON
                }

                // Log activity
                var activity = new ActivityLog
                {
                    ProjectId = project.Id,
                    UserId = currentUser.Id,
                    Action = $"restored project and set status to {model.Status}",
                    TargetType = "Project",
                    TargetId = project.Id.ToString(),
                    TargetName = project.Name,
                    Timestamp = DateTime.UtcNow,
                    AdditionalData = additionalDataJson, // Set AdditionalData to non-null value
                    Type = "ProjectRestore" // Consider adding a type for better filtering
                };
                
                _context.ActivityLogs.Add(activity);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Project {id} '{project.Name}' restored successfully");
                return Ok(new { success = true, message = "Project restored successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restoring project {id}");
                return StatusCode(500, $"An error occurred while restoring the project: {ex.Message}");
            }
        }

        public class RestoreProjectModel
        {
            public DateTime Deadline { get; set; }
            public string Status { get; set; }
            public string Priority { get; set; }
        }

        [HttpGet("ManageMembers/{id}")]
        public async Task<IActionResult> ManageMembers(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                _logger.LogInformation($"Loading members management for project ID: {id}");
                
                var project = await _context.Projects
                    .Include(p => p.ProjectMembers)
                        .ThenInclude(m => m.User)
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == currentUser.CompanyId);

                if (project == null)
                {
                    _logger.LogWarning($"Project with ID {id} not found or user {currentUser.Id} doesn't have access");
                    return NotFound();
                }

                // Check if current user is project lead
                var isProjectLead = project.ProjectMembers
                    .Any(m => m.UserId == currentUser.Id && m.Role == "Project Lead");
                    
                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                // If user is not a project lead or admin, forbid access
                if (!isProjectLead && !isAdmin)
                {
                    return Forbid();
                }

                var viewModel = new ProjectMemberManagementViewModel
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    IsCurrentUserAdmin = isAdmin,
                    Members = project.ProjectMembers.Select(m => new MemberViewModel
                    {
                        UserId = m.User.Id,
                        Name = m.User.Name,
                        LastName = m.User.LastName,
                        ProfilePicturePath = m.User.ProfilePicturePath ?? "/images/default/default-profile.png",
                        Role = m.Role
                    }).ToList()
                };

                _logger.LogInformation($"Successfully built view model for managing members of project {project.Name}");

                // Return partial view for AJAX requests, full view otherwise
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_ManageMembersModalContent", viewModel);
                }

                return View("ManageMembers", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading members management for project with ID {id}");
                return StatusCode(500, $"An error occurred while loading the members: {ex.Message}");
            }
        }

        [HttpGet("{id}/AllActivities")]
        public async Task<IActionResult> GetAllActivities(int id, int limit = 100)
        {
            try
            {
                _logger.LogInformation($"All activities requested for project ID: {id}");
                
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("No current user found");
                    return Unauthorized();
                }

                _logger.LogInformation($"User {currentUser.Id} requesting activities for project {id}");

                // Check if project exists and user has access
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == currentUser.CompanyId);

                if (project == null)
                {
                    _logger.LogWarning($"Project with ID {id} not found or user {currentUser.Id} doesn't have access");
                    return NotFound();
                }

                _logger.LogInformation($"Project {id} found, fetching activities");

                // Get all activities with error handling
                try
                {
                    // Do a basic count first to see if there are any activities at all
                    var basicCount = await _context.ActivityLogs.CountAsync();
                    _logger.LogInformation($"Total activity logs in database: {basicCount}");
                    
                    var projectActivityCount = await _context.ActivityLogs
                        .Where(a => a.ProjectId == id)
                        .CountAsync();
                    _logger.LogInformation($"Activity logs for project {id}: {projectActivityCount}");
                                        
                    // Now try to fetch activities again
                    var activities = await _context.ActivityLogs
                        .Include(a => a.User)
                        .Where(a => a.ProjectId == id)
                        .OrderByDescending(a => a.Timestamp)
                        .Take(limit)
                        .ToListAsync();
                    
                    _logger.LogInformation($"Retrieved {activities.Count} activities after check");
                    
                    // Debug print the first few activities if any
                    if (activities.Any())
                    {
                        foreach (var activity in activities.Take(3))
                        {
                            _logger.LogInformation($"Activity: {activity.Id}, Action: {activity.Action}, User: {activity.UserId}, Time: {activity.Timestamp}");
                        }
                    }
                    
                    var viewModels = activities.Select(a => new ActivityViewModel
                    {
                        Type = a.Type ?? string.Empty,
                        UserId = a.UserId ?? string.Empty,
                        UserName = a.User != null ? $"{a.User.Name} {a.User.LastName}" : "Unknown User",
                        UserAvatar = a.User?.ProfilePicturePath ?? "/images/default/default-profile.png",
                        TimeAgo = GetTimeAgo(a.Timestamp),
                        Description = a.Action ?? string.Empty,
                        TargetType = a.TargetType ?? string.Empty,
                        TargetId = a.TargetId ?? string.Empty,
                        TargetName = a.TargetName ?? string.Empty
                    }).ToList();
                    
                    _logger.LogInformation($"Created {viewModels.Count} view models");
                    
                    // Check if after mapping we have view models
                    if (!viewModels.Any())
                    {
                        _logger.LogWarning("No view models created despite having activities");
                    }
                    
                    // Return partial view for AJAX requests, full view otherwise
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return PartialView("_AllActivitiesContent", viewModels);
                    }

                    return View("_AllActivitiesContent", viewModels);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing all activities for project {id}");
                    // Return an empty list rather than error - this is more user-friendly
                    return PartialView("_AllActivitiesContent", new List<ActivityViewModel>());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unhandled exception in GetAllActivities for project {id}");
                return StatusCode(500, $"An error occurred while loading activities: {ex.Message}");
            }
        }

        [HttpGet("GetAllActivitiesModal")]
        public IActionResult GetAllActivitiesModal()
        {
            return PartialView("_AllActivitiesModal");
        }

    }
}