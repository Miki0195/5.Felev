using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Managly.Data;
using Managly.Models;
using Managly.Models.DTOs.Projects;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Managly.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectsApiController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ProjectsApiController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.IsAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            return View();
        }

        // GET: api/projects
        [HttpGet]
        public async Task<IActionResult> GetProjects()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Unauthorized(new { success = false, message = "User not found" });
                }

                var projects = await _context.Projects
                    .Where(p => p.CompanyId == currentUser.CompanyId)
                    .Select(p => new {
                        id = p.Id,
                        name = p.Name,
                        description = p.Description,
                        startDate = p.StartDate,
                        deadline = p.Deadline,
                        priority = p.Priority,
                        status = p.Status
                    })
                    .ToListAsync();

                return Ok(projects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error fetching projects: " + ex.Message });
            }
        }

        // GET: api/projects/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProject(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var project = await _context.Projects
                    .Include(p => p.ProjectMembers)
                        .ThenInclude(m => m.User)
                    .Include(p => p.CreatedBy)
                    .Include(p => p.Tasks)
                        .ThenInclude(t => t.Assignments)
                            .ThenInclude(a => a.User)
                    .FirstOrDefaultAsync(p => p.Id == id &&
                        (p.CompanyId == currentUser.CompanyId));

                if (project == null)
                    return NotFound();

                // Check if current user is project lead
                var isProjectLead = project.ProjectMembers
                    .Any(m => m.UserId == currentUser.Id && m.Role == "Project Lead");

                var response = new
                {
                    project.Id,
                    project.Name,
                    project.Description,
                    StartDate = project.StartDate.ToString("yyyy-MM-dd"),
                    Deadline = project.Deadline?.ToString("yyyy-MM-dd"),
                    project.Status,
                    project.Priority,
                    project.TotalTasks,
                    project.CompletedTasks,
                    IsProjectLead = isProjectLead,
                    CurrentUserId = currentUser.Id,
                    ProjectMembers = project.ProjectMembers.Select(m => new
                    {
                        m.Role,
                        User = new
                        {
                            m.User.Id,
                            m.User.Name,
                            m.User.LastName,
                            m.User.ProfilePicturePath
                        }
                    }),
                    Tasks = project.Tasks.Select(t => new
                    {
                        t.Id,
                        t.TaskTitle,
                        t.Description,
                        DueDate = t.DueDate.ToString(),
                        t.Priority,
                        t.Status,
                        t.TimeSpent,
                        AssignedUsers = t.Assignments.Select(a => new
                        {
                            a.User.Id,
                            a.User.Name,
                            a.User.LastName,
                            a.User.ProfilePicturePath,
                            a.AssignedAt
                        })
                    })
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/projects/create
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProject([FromBody] ProjectCreateDto projectDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null || !currentUser.CompanyId.HasValue)
                {
                    return BadRequest(new { error = "User or company information not found" });
                }

                //Parse dates with specific format
                if (!DateTime.TryParse(projectDto.StartDate, out DateTime startDate) ||
                    !DateTime.TryParse(projectDto.Deadline, out DateTime deadline))
                {
                    return BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD format." });
                }

                var project = new Project
                {
                    Name = projectDto.Name,
                    Description = projectDto.Description,
                    StartDate = startDate,
                    Deadline = deadline,
                    Status = projectDto.Status,
                    Priority = projectDto.Priority,
                    CreatedById = currentUser.Id,
                    CompanyId = currentUser.CompanyId.Value,
                    CreatedAt = DateTime.UtcNow,
                    ProjectMembers = new List<ProjectMember>(),
                    Tasks = new List<Tasks>(),
                    Activities = new List<ActivityLog>(),
                    TotalTasks = 0,
                    CompletedTasks = 0
                };

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                // Add creator as Project Lead
                var creatorMember = new ProjectMember
                {
                    ProjectId = project.Id,
                    UserId = currentUser.Id,
                    Role = "Project Lead",
                    JoinedAt = DateTime.UtcNow
                };
                _context.ProjectMembers.Add(creatorMember);

                // Add selected team members
                if (projectDto.TeamMemberIds != null && projectDto.TeamMemberIds.Any())
                {
                    foreach (var memberId in projectDto.TeamMemberIds)
                    {
                        var member = new ProjectMember
                        {
                            ProjectId = project.Id,
                            UserId = memberId,
                            Role = "Member",
                            JoinedAt = DateTime.UtcNow
                        };
                        _context.ProjectMembers.Add(member);

                        // Send notifications to team members
                        var notification = new Notification
                        {
                            UserId = memberId,
                            Message = $"You have been added to project: {project.Name}",
                            Link = $"/projects?projectId={project.Id}",
                            IsRead = false
                        };
                        _context.Notifications.Add(notification);
                    }
                }

                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Server error: {ex.Message}" });
            }
        }

        // PUT: api/projectsapi/{id}/update
        [HttpPut("{id}/update")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] ProjectUpdateDto projectDto)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var project = await _context.Projects
                    .Include(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == currentUser.CompanyId);

                if (project == null)
                    return NotFound();

                // Check if user is project lead
                var isProjectLead = project.ProjectMembers
                    .Any(m => m.UserId == currentUser.Id && m.Role == "Project Lead");

                if (!isProjectLead)
                    return Forbid();

                // Track changes to log activity
                bool nameChanged = project.Name != projectDto.Name;
                bool descriptionChanged = project.Description != projectDto.Description;
                bool startDateChanged = project.StartDate.ToString("yyyy-MM-dd") != projectDto.StartDate;
                bool deadlineChanged = project.Deadline?.ToString("yyyy-MM-dd") != projectDto.Deadline;
                bool statusChanged = project.Status != projectDto.Status;
                bool priorityChanged = project.Priority != projectDto.Priority;

                // Store old values for activity log
                string oldName = project.Name;
                string oldDescription = project.Description;
                string oldStartDate = project.StartDate.ToString("yyyy-MM-dd");
                string oldDeadline = project.Deadline?.ToString("yyyy-MM-dd");
                string oldStatus = project.Status;
                string oldPriority = project.Priority;

                // Update project details
                project.Name = projectDto.Name;
                project.Description = projectDto.Description;
                project.StartDate = DateTime.Parse(projectDto.StartDate);
                project.Deadline = DateTime.Parse(projectDto.Deadline);
                project.Status = projectDto.Status;
                project.Priority = projectDto.Priority;
                project.UpdatedAt = DateTime.UtcNow;

                // Set CompletedAt when a project is marked as completed
                if (statusChanged && project.Status == "Completed")
                {
                    project.CompletedAt = DateTime.UtcNow;
                }

                // Create a summary of all changes
                var allChanges = new Dictionary<string, object>();
                if (nameChanged) allChanges.Add("name", new { oldValue = oldName, newValue = project.Name });
                if (descriptionChanged) allChanges.Add("description", new { oldValue = oldDescription, newValue = project.Description });
                if (startDateChanged) allChanges.Add("startDate", new { oldValue = oldStartDate, newValue = projectDto.StartDate });
                if (deadlineChanged) allChanges.Add("deadline", new { oldValue = oldDeadline, newValue = projectDto.Deadline });
                if (statusChanged) allChanges.Add("status", new { oldValue = oldStatus, newValue = project.Status });
                if (priorityChanged) allChanges.Add("priority", new { oldValue = oldPriority, newValue = project.Priority });

                // Add a general project update activity with all changes
                if (allChanges.Count > 0)
                {
                    var activity = new ActivityLog
                    {
                        ProjectId = project.Id,
                        UserId = currentUser.Id,
                        Action = "updated project details",
                        TargetType = "Project",
                        TargetId = project.Id.ToString(),
                        TargetName = project.Name,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = JsonSerializer.Serialize(allChanges)
                    };
                    _context.ActivityLogs.Add(activity);
                }

                // Log activities for significant changes with detailed information
                if (nameChanged)
                {
                    var activity = new ActivityLog
                    {
                        ProjectId = project.Id,
                        UserId = currentUser.Id,
                        Action = "changed project name",
                        TargetType = "Project",
                        TargetId = project.Id.ToString(),
                        TargetName = project.Name,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = JsonSerializer.Serialize(new { oldName = oldName, newName = project.Name })
                    };
                    _context.ActivityLogs.Add(activity);
                }

                if (deadlineChanged)
                {
                    var activity = new ActivityLog
                    {
                        ProjectId = project.Id,
                        UserId = currentUser.Id,
                        Action = "updated project deadline",
                        TargetType = "Project",
                        TargetId = project.Id.ToString(),
                        TargetName = project.Name,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = JsonSerializer.Serialize(new { oldDeadline = oldDeadline, newDeadline = projectDto.Deadline })
                    };
                    _context.ActivityLogs.Add(activity);
                }

                if (statusChanged)
                {
                    var activity = new ActivityLog
                    {
                        ProjectId = project.Id,
                        UserId = currentUser.Id,
                        Action = $"changed project status from {oldStatus} to {project.Status}",
                        TargetType = "Project",
                        TargetId = project.Id.ToString(),
                        TargetName = project.Name,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = JsonSerializer.Serialize(new { oldStatus = oldStatus, newStatus = project.Status })
                    };
                    _context.ActivityLogs.Add(activity);
                }

                if (priorityChanged)
                {
                    var activity = new ActivityLog
                    {
                        ProjectId = project.Id,
                        UserId = currentUser.Id,
                        Action = $"changed project priority from {oldPriority} to {project.Priority}",
                        TargetType = "Project",
                        TargetId = project.Id.ToString(),
                        TargetName = project.Name,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = JsonSerializer.Serialize(new { oldPriority = oldPriority, newPriority = project.Priority })
                    };
                    _context.ActivityLogs.Add(activity);
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE: api/projects/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var project = await _context.Projects
                    .Include(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == currentUser.CompanyId);

                if (project == null)
                    return NotFound();

                // Check if user is project lead
                var isProjectLead = project.ProjectMembers
                    .Any(m => m.UserId == currentUser.Id && m.Role == "Project Lead");

                if (!isProjectLead)
                    return Forbid();

                // Delete project members first
                _context.ProjectMembers.RemoveRange(project.ProjectMembers);

                // Delete the project
                _context.Projects.Remove(project);

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/projects/company-users
        [HttpGet("company-users")]
        public async Task<IActionResult> GetCompanyUsers()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var users = await _context.Users
                .Where(u => u.CompanyId == currentUser.CompanyId && u.Id != currentUser.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.LastName,
                    u.ProfilePicturePath
                })
                .ToListAsync();

            return Ok(users);
        }

        // POST: api/projectsapi/{id}/members
        [HttpPost("{id}/members")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddProjectMembers(int id, [FromBody] AddMembersDto membersDto)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var project = await _context.Projects
                    .Include(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == currentUser.CompanyId);

                if (project == null)
                    return NotFound();

                // Check if user is project lead
                var isProjectLead = project.ProjectMembers
                    .Any(m => m.UserId == currentUser.Id && m.Role == "Project Lead");

                if (!isProjectLead)
                    return Forbid();

                // Get users being added for activity log
                var usersBeingAdded = await _context.Users
                    .Where(u => membersDto.MemberIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.Name, u.LastName })
                    .ToListAsync();

                // Add new members
                foreach (var memberId in membersDto.MemberIds)
                {
                    if (!project.ProjectMembers.Any(m => m.UserId == memberId))
                    {
                        var member = new ProjectMember
                        {
                            ProjectId = project.Id,
                            UserId = memberId,
                            Role = "Member",
                            JoinedAt = DateTime.UtcNow
                        };
                        _context.ProjectMembers.Add(member);

                        // Get user details for the activity log
                        var user = usersBeingAdded.FirstOrDefault(u => u.Id == memberId);
                        if (user != null)
                        {
                            // Log member addition with detailed information
                            var activity = new ActivityLog
                            {
                                ProjectId = project.Id,
                                UserId = currentUser.Id,
                                Action = $"added {user.Name} {user.LastName} to project",
                                TargetType = "Member",
                                TargetId = memberId,
                                TargetName = $"{user.Name} {user.LastName}",
                                Timestamp = DateTime.UtcNow,
                                AdditionalData = JsonSerializer.Serialize(new
                                {
                                    memberId = memberId,
                                    memberName = $"{user.Name} {user.LastName}",
                                    role = "Member",
                                    addedBy = $"{currentUser.Name} {currentUser.LastName}",
                                    addedById = currentUser.Id
                                })
                            };
                            _context.ActivityLogs.Add(activity);

                            // Add a notification for the user
                            var notification = new Notification
                            {
                                UserId = memberId,
                                Message = $"You have been added to project: {project.Name}",
                                Link = $"/projects?projectId={project.Id}",
                                IsRead = false
                            };
                            _context.Notifications.Add(notification);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{id}/members/{userId}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMemberRole(int id, string userId, [FromBody] UpdateRoleDto roleDto)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var projectMember = await _context.ProjectMembers
                    .Include(pm => pm.Project)
                    .Include(pm => pm.User)
                    .FirstOrDefaultAsync(pm => pm.ProjectId == id &&
                                             pm.UserId == userId &&
                                             pm.Project.CompanyId == currentUser.CompanyId);

                if (projectMember == null)
                    return NotFound();

                // Check if current user is project lead
                var isProjectLead = await _context.ProjectMembers
                    .AnyAsync(m => m.ProjectId == id &&
                                  m.UserId == currentUser.Id &&
                                  m.Role == "Project Lead");

                if (!isProjectLead)
                    return Forbid();

                // Don't allow modifying Project Lead's role
                if (projectMember.Role == "Project Lead")
                    return BadRequest(new { error = "Cannot modify Project Lead's role" });

                // Store the old role for logging
                string oldRole = projectMember.Role;

                // Update the role
                projectMember.Role = roleDto.Role;

                // Log role change
                var activity = new ActivityLog
                {
                    ProjectId = id,
                    UserId = currentUser.Id,
                    Action = $"changed {projectMember.User.Name} {projectMember.User.LastName}'s role from {oldRole} to {roleDto.Role}",
                    TargetType = "Member",
                    TargetId = userId,
                    TargetName = $"{projectMember.User.Name} {projectMember.User.LastName}",
                    Timestamp = DateTime.UtcNow,
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        memberId = userId,
                        memberName = $"{projectMember.User.Name} {projectMember.User.LastName}",
                        oldRole = oldRole,
                        newRole = roleDto.Role,
                        changedBy = $"{currentUser.Name} {currentUser.LastName}",
                        changedById = currentUser.Id
                    })
                };
                _context.ActivityLogs.Add(activity);

                // Add notification for the user
                var notification = new Notification
                {
                    UserId = userId,
                    Message = $"Your role in project '{projectMember.Project.Name}' has been changed to {roleDto.Role}",
                    Link = $"/projects?projectId={id}",
                    IsRead = false
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{id}/members/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveMember(int id, string userId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var projectMember = await _context.ProjectMembers
                    .Include(pm => pm.Project)
                    .Include(pm => pm.User)
                    .FirstOrDefaultAsync(pm => pm.ProjectId == id &&
                                             pm.UserId == userId &&
                                             pm.Project.CompanyId == currentUser.CompanyId);

                if (projectMember == null)
                    return NotFound();

                // Check if current user is project lead
                var isProjectLead = await _context.ProjectMembers
                    .AnyAsync(m => m.ProjectId == id &&
                                  m.UserId == currentUser.Id &&
                                  m.Role == "Project Lead");

                if (!isProjectLead)
                    return Forbid();

                // Don't allow removing Project Lead
                if (projectMember.Role == "Project Lead")
                    return BadRequest(new { error = "Cannot remove Project Lead from project" });

                // Store member details for logging
                string memberName = $"{projectMember.User.Name} {projectMember.User.LastName}";
                string memberRole = projectMember.Role;

                // Log member removal
                var activity = new ActivityLog
                {
                    ProjectId = id,
                    UserId = currentUser.Id,
                    Action = $"removed {memberName} from project",
                    TargetType = "Member",
                    TargetId = userId,
                    TargetName = memberName,
                    Timestamp = DateTime.UtcNow,
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        memberId = userId,
                        memberName = memberName,
                        role = memberRole,
                        removedBy = $"{currentUser.Name} {currentUser.LastName}",
                        removedById = currentUser.Id,
                        projectName = projectMember.Project.Name
                    })
                };
                _context.ActivityLogs.Add(activity);

                // Add notification for the user
                var notification = new Notification
                {
                    UserId = userId,
                    Message = $"You have been removed from project '{projectMember.Project.Name}'",
                    Link = "/projects", // Cannot link to specific project since they've been removed
                    IsRead = false
                };
                _context.Notifications.Add(notification);

                // Remove the member
                _context.ProjectMembers.Remove(projectMember);

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        

        public class ProjectUpdateDto
        {
            [Required]
            public string Name { get; set; }
            public string Description { get; set; }
            [Required]
            public string StartDate { get; set; }
            [Required]
            public string Deadline { get; set; }
            [Required]
            public string Status { get; set; }
            [Required]
            public string Priority { get; set; }
        }

        public class AddMembersDto
        {
            [Required]
            public List<string> MemberIds { get; set; }
        }

        public class UpdateRoleDto
        {
            [Required]
            public string Role { get; set; }
        }

        // Add these DTOs at the bottom of the file
        public class TaskCreateDto
        {
            [Required]
            public string TaskTitle { get; set; }

            public string Description { get; set; }

            [Required]
            public string DueDate { get; set; }

            [Required]
            public string Priority { get; set; }

            [Required]
            public string AssignedToId { get; set; }
        }

        // Add these endpoints to the controller
        [HttpPost("{id}/tasks")]
        [Authorize]
        public async Task<IActionResult> CreateTask(int id, [FromBody] TaskCreateDto taskDto)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var project = await _context.Projects
                    .Include(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == currentUser.CompanyId);

                if (project == null)
                    return NotFound();

                // Check if user is project lead or manager
                var userRole = project.ProjectMembers
                    .FirstOrDefault(m => m.UserId == currentUser.Id)?.Role;

                if (userRole != "Project Lead" && userRole != "Manager")
                    return Forbid();

                // Get assigned user details
                var assignedUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == taskDto.AssignedToId);

                if (assignedUser == null)
                    return BadRequest(new { error = "Assigned user not found" });

                // Check if assigned user is a project member
                if (!project.ProjectMembers.Any(m => m.UserId == taskDto.AssignedToId))
                    return BadRequest(new { error = $"User is not a project member" });

                if (!DateTime.TryParse(taskDto.DueDate, out DateTime dueDate))
                    return BadRequest(new { error = "Invalid date format" });

                // Create the task
                var task = new Tasks
                {
                    ProjectId = id,
                    TaskTitle = taskDto.TaskTitle,
                    Description = taskDto.Description,
                    DueDate = dueDate,
                    Priority = taskDto.Priority,
                    Status = "Pending",
                    CreatedById = currentUser.Id,
                    AssignedDate = DateTime.UtcNow
                };

                _context.Tasks.Add(task);
                await _context.SaveChangesAsync(); // Save first to get the task ID

                // Create the task assignment
                var assignment = new TaskAssignment
                {
                    TaskId = task.Id,
                    UserId = taskDto.AssignedToId,
                    AssignedAt = DateTime.UtcNow
                };
                _context.TaskAssignments.Add(assignment);

                project.TotalTasks++;

                // Log task creation activity with detailed information
                var createActivity = new ActivityLog
                {
                    ProjectId = project.Id,
                    UserId = currentUser.Id,
                    Action = "created task",
                    TargetType = "Task",
                    TargetId = task.Id.ToString(),
                    TargetName = task.TaskTitle,
                    Timestamp = DateTime.UtcNow,
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        taskId = task.Id,
                        taskTitle = task.TaskTitle,
                        description = task.Description,
                        dueDate = task.DueDate?.ToString("yyyy-MM-dd"),
                        priority = task.Priority,
                        status = task.Status,
                        createdBy = $"{currentUser.Name} {currentUser.LastName}",
                        createdById = currentUser.Id,
                        projectId = project.Id,
                        projectName = project.Name
                    })
                };
                _context.ActivityLogs.Add(createActivity);

                // Log task assignment activity with detailed information
                var assignActivity = new ActivityLog
                {
                    ProjectId = project.Id,
                    UserId = currentUser.Id,
                    Action = $"assigned task to {assignedUser.Name} {assignedUser.LastName}",
                    TargetType = "Task",
                    TargetId = task.Id.ToString(),
                    TargetName = task.TaskTitle,
                    Timestamp = DateTime.UtcNow,
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        taskId = task.Id,
                        taskTitle = task.TaskTitle,
                        assignedTo = assignedUser.Id,
                        assignedToName = $"{assignedUser.Name} {assignedUser.LastName}",
                        assignedBy = currentUser.Id,
                        assignedByName = $"{currentUser.Name} {currentUser.LastName}",
                        assignedAt = DateTime.UtcNow
                    })
                };
                _context.ActivityLogs.Add(assignActivity);

                // Send notification to assigned user
                var notification = new Notification
                {
                    UserId = taskDto.AssignedToId,
                    Message = $"You have been assigned a new task: {task.TaskTitle}",
                    Link = $"/projects?projectId={id}&taskId={task.Id}",
                    IsRead = false
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                // Get assigned user details
                var assignedUserDetails = await _context.Users
                    .Where(u => u.Id == taskDto.AssignedToId)
                    .Select(u => new
                    {
                        u.Id,
                        u.Name,
                        u.LastName,
                        u.ProfilePicturePath
                    })
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    task.Id,
                    task.TaskTitle,
                    task.Description,
                    DueDate = task.DueDate?.ToString("yyyy-MM-dd"),
                    task.Priority,
                    task.Status,
                    AssignedUsers = new[] { assignedUserDetails }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}/tasks")]
        [Authorize]
        public async Task<IActionResult> GetProjectTasks(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var tasks = await _context.Tasks
                    .Include(t => t.Assignments)
                        .ThenInclude(a => a.User)
                    .Include(t => t.CreatedBy)
                    .Where(t => t.ProjectId == id && t.Project.CompanyId == currentUser.CompanyId)
                    .Select(t => new
                    {
                        t.Id,
                        t.TaskTitle,
                        t.Description,
                        DueDate = t.DueDate.ToString(),
                        t.Priority,
                        t.Status,
                        t.TimeSpent,
                        AssignedUsers = t.Assignments.Select(a => new
                        {
                            a.User.Id,
                            a.User.Name,
                            a.User.LastName,
                            a.User.ProfilePicturePath,
                            a.AssignedAt
                        }),
                        CreatedBy = new
                        {
                            t.CreatedBy.Id,
                            t.CreatedBy.Name,
                            t.CreatedBy.LastName,
                            t.CreatedBy.ProfilePicturePath
                        },
                        t.AssignedDate
                    })
                    .OrderByDescending(t => t.AssignedDate)
                    .ToListAsync();

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("tasks/{id}")]
        [Authorize]
        public async Task<IActionResult> GetTaskDetails(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var task = await _context.Tasks
                    .Include(t => t.Assignments)
                        .ThenInclude(a => a.User)
                    .Include(t => t.CreatedBy)
                    .Include(t => t.Project)
                    .FirstOrDefaultAsync(t => t.Id == id && t.Project.CompanyId == currentUser.CompanyId);

                if (task == null)
                    return NotFound();

                return Ok(new
                {
                    task.Id,
                    task.TaskTitle,
                    task.Description,
                    DueDate = task.DueDate.ToString(),
                    task.Priority,
                    task.Status,
                    task.TimeSpent,
                    AssignedUsers = task.Assignments.Select(a => new
                    {
                        a.User.Id,
                        a.User.Name,
                        a.User.LastName,
                        a.User.ProfilePicturePath,
                        a.AssignedAt
                    }),
                    CreatedBy = new
                    {
                        task.CreatedBy.Id,
                        task.CreatedBy.Name,
                        task.CreatedBy.LastName,
                        task.CreatedBy.ProfilePicturePath
                    },
                    task.AssignedDate,
                    task.ProjectId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("tasks/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskUpdateDto taskDto)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var task = await _context.Tasks
                    .Include(t => t.Project)
                        .ThenInclude(p => p.ProjectMembers)
                    .Include(t => t.Assignments)
                    .FirstOrDefaultAsync(t => t.Id == id && t.Project.CompanyId == currentUser.CompanyId);

                if (task == null)
                    return NotFound();

                // Check if user is project lead or manager
                var userRole = task.Project.ProjectMembers
                    .FirstOrDefault(m => m.UserId == currentUser.Id)?.Role;

                if (userRole != "Project Lead" && userRole != "Manager")
                    return Forbid();

                // Get existing assignments before update
                var existingAssignments = task.Assignments.Select(a => a.UserId).ToList();

                // Store original values for activity logging
                string oldTitle = task.TaskTitle;
                string oldDescription = task.Description;
                DateTime oldDueDate = (DateTime)task.DueDate;
                string oldPriority = task.Priority;
                string oldStatus = task.Status;

                // Check if important properties are being changed
                bool titleChanged = task.TaskTitle != taskDto.TaskTitle;
                bool descriptionChanged = task.Description != taskDto.Description;
                bool dueDateChanged = task.DueDate != DateTime.Parse(taskDto.DueDate);
                bool priorityChanged = task.Priority != taskDto.Priority;
                bool statusChanged = task.Status != taskDto.Status;

                // Track assignment changes
                var newAssignments = taskDto.AssignedUserIds.Except(existingAssignments).ToList();
                var removedAssignments = existingAssignments.Except(taskDto.AssignedUserIds).ToList();
                bool assignmentsChanged = newAssignments.Any() || removedAssignments.Any();

                // Update task details
                task.TaskTitle = taskDto.TaskTitle;
                task.Description = taskDto.Description;
                task.DueDate = DateTime.Parse(taskDto.DueDate);
                task.Priority = taskDto.Priority;
                task.Status = taskDto.Status;

                // Create changes dictionary for activity log
                var changes = new Dictionary<string, object>();
                if (titleChanged) changes.Add("title", new { oldValue = oldTitle, newValue = task.TaskTitle });
                if (descriptionChanged) changes.Add("description", new { oldValue = oldDescription, newValue = task.Description });
                if (dueDateChanged) changes.Add("dueDate", new { oldValue = oldDueDate.ToString("yyyy-MM-dd"), newValue = task.DueDate?.ToString("yyyy-MM-dd") });
                if (priorityChanged) changes.Add("priority", new { oldValue = oldPriority, newValue = task.Priority });
                if (statusChanged) changes.Add("status", new { oldValue = oldStatus, newValue = task.Status });
                if (assignmentsChanged)
                {
                    var assignmentChanges = new
                    {
                        added = newAssignments,
                        removed = removedAssignments
                    };
                    changes.Add("assignments", assignmentChanges);
                }

                // Log task update activity with detailed AdditionalData
                if (changes.Count > 0)
                {
                    var updateActivity = new ActivityLog
                    {
                        ProjectId = task.Project.Id,
                        UserId = currentUser.Id,
                        Action = "updated task",
                        TargetType = "Task",
                        TargetId = task.Id.ToString(),
                        TargetName = task.TaskTitle,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = JsonSerializer.Serialize(changes)
                    };
                    _context.ActivityLogs.Add(updateActivity);
                }

                // For status changes, create a specific status update activity
                if (statusChanged)
                {
                    var statusActivity = new ActivityLog
                    {
                        ProjectId = task.Project.Id,
                        UserId = currentUser.Id,
                        Action = $"changed task status from {oldStatus} to {task.Status}",
                        TargetType = "Task",
                        TargetId = task.Id.ToString(),
                        TargetName = task.TaskTitle,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = JsonSerializer.Serialize(new { oldStatus, newStatus = task.Status })
                    };
                    _context.ActivityLogs.Add(statusActivity);
                }

                // Update task assignments
                _context.TaskAssignments.RemoveRange(task.Assignments);
                task.Assignments.Clear();

                // Get user details for logging
                var userDetails = await _context.Users
                    .Where(u => taskDto.AssignedUserIds.Contains(u.Id) || removedAssignments.Contains(u.Id))
                    .Select(u => new { u.Id, Name = $"{u.Name} {u.LastName}" })
                    .ToDictionaryAsync(u => u.Id, u => u.Name);

                // Add new assignments
                foreach (var userId in taskDto.AssignedUserIds)
                {
                    var assignment = new TaskAssignment
                    {
                        TaskId = task.Id,
                        UserId = userId,
                        AssignedAt = DateTime.UtcNow
                    };
                    task.Assignments.Add(assignment);

                    // Send notification only to newly assigned users
                    if (!existingAssignments.Contains(userId))
                    {
                        var notification = new Notification
                        {
                            UserId = userId,
                            Message = $"You have been assigned to task: {task.TaskTitle}",
                            Link = $"/projects?projectId={task.ProjectId}&taskId={task.Id}",
                            IsRead = false
                        };
                        _context.Notifications.Add(notification);

                        // Log assignment activity with detailed information
                        var userName = userDetails.ContainsKey(userId) ? userDetails[userId] : "Unknown User";
                        var assignActivity = new ActivityLog
                        {
                            ProjectId = task.Project.Id,
                            UserId = currentUser.Id,
                            Action = $"assigned {userName} to task",
                            TargetType = "Task",
                            TargetId = task.Id.ToString(),
                            TargetName = task.TaskTitle,
                            Timestamp = DateTime.UtcNow,
                            AdditionalData = JsonSerializer.Serialize(new
                            {
                                assignedTo = userId,
                                assignedToName = userName,
                                assignedBy = currentUser.Id,
                                assignedByName = $"{currentUser.Name} {currentUser.LastName}"
                            })
                        };
                        _context.ActivityLogs.Add(assignActivity);
                    }
                }

                // Log removal of assignments
                foreach (var userId in removedAssignments)
                {
                    var userName = userDetails.ContainsKey(userId) ? userDetails[userId] : "Unknown User";
                    var removeActivity = new ActivityLog
                    {
                        ProjectId = task.Project.Id,
                        UserId = currentUser.Id,
                        Action = $"removed {userName} from task",
                        TargetType = "Task",
                        TargetId = task.Id.ToString(),
                        TargetName = task.TaskTitle,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = JsonSerializer.Serialize(new
                        {
                            removedUser = userId,
                            removedUserName = userName,
                            removedBy = currentUser.Id,
                            removedByName = $"{currentUser.Name} {currentUser.LastName}"
                        })
                    };
                    _context.ActivityLogs.Add(removeActivity);
                }

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("tasks/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteTask(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var task = await _context.Tasks
                    .Include(t => t.Project)
                        .ThenInclude(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(t => t.Id == id && t.Project.CompanyId == currentUser.CompanyId);

                if (task == null)
                    return NotFound();

                // Check if user is project lead or manager
                var userRole = task.Project.ProjectMembers
                    .FirstOrDefault(m => m.UserId == currentUser.Id)?.Role;

                if (userRole != "Project Lead" && userRole != "Manager")
                    return Forbid();

                _context.Tasks.Remove(task);
                task.Project.TotalTasks--;
                if (task.Status == "Completed")
                    task.Project.CompletedTasks--;

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public class TaskUpdateDto
        {
            [Required]
            public string TaskTitle { get; set; }

            public string Description { get; set; }

            [Required]
            public string DueDate { get; set; }

            [Required]
            public string Priority { get; set; }

            [Required]
            public string Status { get; set; }

            [Required]
            public List<string> AssignedUserIds { get; set; }
        }

        public class TaskStatusUpdateDto
        {
            [Required]
            public string Status { get; set; }
        }

        [HttpPut("tasks/{id}/status")]
        [Authorize]
        public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] TaskStatusUpdateDto statusDto)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized(new { error = "User not found" });

                var task = await _context.Tasks
                    .Include(t => t.Project)
                        .ThenInclude(p => p.ProjectMembers)
                    .Include(t => t.Assignments)
                    .FirstOrDefaultAsync(t => t.Id == id && t.Project.CompanyId == currentUser.CompanyId);

                if (task == null)
                    return NotFound(new { error = "Task not found or does not belong to your company" });

                // Check if the current user is assigned to this task
                if (!task.Assignments.Any(a => a.UserId == currentUser.Id))
                    return Forbid(new { error = "You are not assigned to this task" });

                var oldStatus = task.Status;
                task.Status = statusDto.Status;

                // Update project progress
                if (oldStatus != "Completed" && statusDto.Status == "Completed")
                {
                    task.Project.CompletedTasks++;

                    // Log task completion activity with detailed information
                    var activity = new ActivityLog
                    {
                        ProjectId = task.Project.Id,
                        UserId = currentUser.Id,
                        Action = "completed task",
                        TargetType = "Task",
                        TargetId = task.Id.ToString(),
                        TargetName = task.TaskTitle,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = JsonSerializer.Serialize(new
                        {
                            taskId = task.Id,
                            taskTitle = task.TaskTitle,
                            oldStatus = oldStatus,
                            newStatus = statusDto.Status,
                            completedBy = currentUser.Id,
                            completedByName = $"{currentUser.Name} {currentUser.LastName}",
                            completedAt = DateTime.UtcNow
                        })
                    };
                    _context.ActivityLogs.Add(activity);
                }
                else if (oldStatus == "Completed" && statusDto.Status != "Completed")
                {
                    task.Project.CompletedTasks--;

                    // Log task status update with detailed information
                    var activity = new ActivityLog
                    {
                        ProjectId = task.Project.Id,
                        UserId = currentUser.Id,
                        Action = $"reopened task and changed status to {statusDto.Status}",
                        TargetType = "Task",
                        TargetId = task.Id.ToString(),
                        TargetName = task.TaskTitle,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = JsonSerializer.Serialize(new
                        {
                            taskId = task.Id,
                            taskTitle = task.TaskTitle,
                            oldStatus = oldStatus,
                            newStatus = statusDto.Status,
                            reopenedBy = currentUser.Id,
                            reopenedByName = $"{currentUser.Name} {currentUser.LastName}",
                            reopenedAt = DateTime.UtcNow
                        })
                    };
                    _context.ActivityLogs.Add(activity);
                }
                else if (oldStatus != statusDto.Status)
                {
                    // Log other status changes with detailed information
                    var activity = new ActivityLog
                    {
                        ProjectId = task.Project.Id,
                        UserId = currentUser.Id,
                        Action = $"changed task status from {oldStatus} to {statusDto.Status}",
                        TargetType = "Task",
                        TargetId = task.Id.ToString(),
                        TargetName = task.TaskTitle,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = JsonSerializer.Serialize(new
                        {
                            taskId = task.Id,
                            taskTitle = task.TaskTitle,
                            oldStatus = oldStatus,
                            newStatus = statusDto.Status,
                            changedBy = currentUser.Id,
                            changedByName = $"{currentUser.Name} {currentUser.LastName}",
                            changedAt = DateTime.UtcNow
                        })
                    };
                    _context.ActivityLogs.Add(activity);
                }

                // If task is completed, notify project lead
                if (statusDto.Status == "Completed")
                {
                    var projectLead = task.Project.ProjectMembers
                        .FirstOrDefault(m => m.Role == "Project Lead");

                    if (projectLead != null)
                    {
                        var notification = new Notification
                        {
                            UserId = projectLead.UserId,
                            Message = $"Task '{task.TaskTitle}' has been marked as completed",
                            Link = $"/projects?projectId={task.ProjectId}&taskId={task.Id}",
                            IsRead = false
                        };

                        _context.Notifications.Add(notification);
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Task status updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating task status: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        private IActionResult Forbid(object value)
        {
            throw new NotImplementedException();
        }

        [HttpGet("team-members")]
        [Authorize]
        public async Task<IActionResult> GetTeamMembers()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var teamMembers = await _context.Users
                    .Where(u => u.CompanyId == currentUser.CompanyId)
                    .Select(u => new
                    {
                        id = u.Id,
                        name = u.Name,
                        lastName = u.LastName,
                        profilePicturePath = u.ProfilePicturePath
                    })
                    .ToListAsync();

                return Ok(teamMembers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("archived")]
        public async Task<IActionResult> GetArchivedProjects()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Unauthorized(new { success = false, message = "User not found" });
                }

                var projects = await _context.Projects
                    .Where(p => p.CompanyId == currentUser.CompanyId &&
                              p.Status == "Completed")
                    .Select(p => new {
                        id = p.Id,
                        name = p.Name,
                        description = p.Description,
                        startDate = p.StartDate,
                        deadline = p.Deadline,
                        priority = p.Priority
                    })
                    .ToListAsync();

                return Ok(projects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error fetching archived projects: " + ex.Message });
            }
        }

        [HttpGet("counts")]
        public async Task<IActionResult> GetProjectCounts()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Unauthorized(new { success = false, message = "User not found" });
                }

                var activeCount = await _context.Projects
                    .Where(p => p.CompanyId == currentUser.CompanyId && p.Status != "Completed")
                    .CountAsync();

                var archivedCount = await _context.Projects
                    .Where(p => p.CompanyId == currentUser.CompanyId && p.Status == "Completed")
                    .CountAsync();

                return Ok(new
                {
                    active = activeCount,
                    archived = archivedCount,
                    total = activeCount + archivedCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error fetching project counts: " + ex.Message });
            }
        }

        // GET: api/projectsapi/{id}/activities
        [HttpGet("{id}/activities")]
        public async Task<IActionResult> GetProjectActivities(int id, [FromQuery] int limit = 20)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new { success = false, message = "User not found" });
                }

                // Check if project exists and user has access
                var project = await _context.Projects
                    .Include(p => p.CreatedBy)
                    .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == currentUser.CompanyId);

                if (project == null)
                {
                    return NotFound(new { success = false, message = "Project not found" });
                }

                // Fetch real activity logs
                var activityLogs = await _context.ActivityLogs
                    .Include(a => a.User)
                    .Where(a => a.ProjectId == id)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .ToListAsync();

                var activities = new List<object>();

                // If we have real activity logs, use them
                if (activityLogs.Any())
                {
                    foreach (var log in activityLogs)
                    {
                        string activityType = "default";

                        if (log.TargetType == "Task")
                        {
                            if (log.Action.Contains("created"))
                                activityType = "task_created";
                            else if (log.Action.Contains("assigned"))
                                activityType = "task_assigned";
                            else if (log.Action.Contains("completed"))
                                activityType = "task_completed";
                            else
                                activityType = "task_updated";
                        }
                        else if (log.TargetType == "Project")
                        {
                            if (log.Action.Contains("created"))
                                activityType = "project_created";
                            else
                                activityType = "project_updated";
                        }
                        else if (log.TargetType == "Member")
                        {
                            activityType = "member_joined";
                        }

                        activities.Add(new
                        {
                            type = activityType,
                            userId = log.UserId,
                            userName = $"{log.User.Name} {log.User.LastName}",
                            userAvatar = log.User.ProfilePicturePath,
                            timestamp = log.Timestamp,
                            description = log.Action,
                            targetType = log.TargetType,
                            targetId = log.TargetId,
                            targetName = log.TargetName
                        });
                    }
                }
                else
                {
                    // For demo purposes, generate some sample activities if no real ones exist
                    // Add some sample task activities based on actual tasks
                    var tasks = await _context.Tasks
                        .Include(t => t.CreatedBy)
                        .Include(t => t.Assignments)
                            .ThenInclude(a => a.User)
                        .Where(t => t.ProjectId == id)
                        .OrderByDescending(t => t.AssignedDate)
                        .Take(5)
                        .ToListAsync();

                    foreach (var task in tasks)
                    {
                        // Task creation activity
                        activities.Add(new
                        {
                            type = "task_created",
                            taskId = task.Id,
                            taskTitle = task.TaskTitle,
                            userId = task.CreatedById,
                            userName = $"{task.CreatedBy.Name} {task.CreatedBy.LastName}",
                            userAvatar = task.CreatedBy.ProfilePicturePath,
                            timestamp = task.AssignedDate,
                            description = $"created task \"{task.TaskTitle}\""
                        });

                        // Task assignment activities
                        foreach (var assignment in task.Assignments)
                        {
                            activities.Add(new
                            {
                                type = "task_assigned",
                                taskId = task.Id,
                                taskTitle = task.TaskTitle,
                                userId = task.CreatedById, // Who assigned the task
                                userName = $"{task.CreatedBy.Name} {task.CreatedBy.LastName}",
                                userAvatar = task.CreatedBy.ProfilePicturePath,
                                targetId = assignment.UserId, // Who was assigned
                                targetName = $"{assignment.User.Name} {assignment.User.LastName}",
                                targetAvatar = assignment.User.ProfilePicturePath,
                                timestamp = assignment.AssignedAt,
                                description = $"assigned \"{task.TaskTitle}\" to {assignment.User.Name} {assignment.User.LastName}"
                            });
                        }
                    }

                    // Add project member activities
                    var members = await _context.ProjectMembers
                        .Include(m => m.User)
                        .Where(m => m.ProjectId == id)
                        .ToListAsync();

                    foreach (var member in members)
                    {
                        activities.Add(new
                        {
                            type = "member_joined",
                            userId = member.UserId,
                            userName = $"{member.User.Name} {member.User.LastName}",
                            userAvatar = member.User.ProfilePicturePath,
                            role = member.Role,
                            timestamp = member.JoinedAt,
                            description = member.Role == "Project Lead"
                                ? $"became Project Lead"
                                : $"joined the project team"
                        });
                    }

                    // Add project creation activity
                    activities.Add(new
                    {
                        type = "project_created",
                        userId = project.CreatedById,
                        userName = $"{project.CreatedBy.Name} {project.CreatedBy.LastName}",
                        userAvatar = project.CreatedBy.ProfilePicturePath,
                        timestamp = project.CreatedAt,
                        description = $"created this project"
                    });
                }

                // Sort by timestamp and take the requested number of records
                var sortedActivities = activities
                    .OrderByDescending(a => ((dynamic)a).timestamp)
                    .Take(limit)
                    .ToList();

                return Ok(new { success = true, activities = sortedActivities });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error fetching project activities: " + ex.Message });
            }
        }

        // ===================== OWN CODE ================ //
        [HttpGet("sidebar")]
        public async Task<IActionResult> GetProjectsForSidebar()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return Unauthorized(new { success = false, message = "User not found" });
                }

                var projects = await _context.Projects
                    .Where(p => p.CompanyId == currentUser.CompanyId)
                    .Select(p => new {
                        id = p.Id,
                        name = p.Name,
                        description = p.Description,
                        startDate = p.StartDate,
                        deadline = p.Deadline,
                        priority = p.Priority,
                        status = p.Status,
                        totalTasks = p.TotalTasks,
                        completedTasks = p.CompletedTasks
                    })
                    .ToListAsync();

                // Sort projects on the server side
                var sortedProjects = projects
                    .OrderBy(p => p.status == "In Progress" ? 0 :
                                  p.status == "Not Started" ? 1 :
                                  p.status == "Completed" ? 2 : 3)
                    .ToList();

                return Ok(sortedProjects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error fetching projects: " + ex.Message });
            }
        }


        
    }
}