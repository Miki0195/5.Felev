using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Managly.Data;
using Managly.Models;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

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
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            var projects = await _context.Projects
                .Where(p => p.CompanyId == currentUser.CompanyId &&
                    (isAdmin || p.ProjectMembers.Any(m => m.UserId == currentUser.Id)))
                .Include(p => p.ProjectMembers)
                    .ThenInclude(m => m.User)
                .Include(p => p.CreatedBy)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.StartDate,
                    p.Deadline,
                    p.Status,
                    p.Priority,
                    p.CreatedAt,
                    Progress = p.TotalTasks == 0 ? 0 : (p.CompletedTasks * 100 / p.TotalTasks),
                    CreatedBy = new
                    {
                        p.CreatedBy.Id,
                        p.CreatedBy.Name,
                        p.CreatedBy.LastName,
                        p.CreatedBy.ProfilePicturePath
                    },
                    Members = p.ProjectMembers.Select(m => new
                    {
                        m.User.Id,
                        m.User.Name,
                        m.User.LastName,
                        m.User.ProfilePicturePath,
                        m.Role,
                        m.JoinedAt
                    })
                })
                .ToListAsync();

            return Ok(projects);
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
                    Deadline = project.Deadline.ToString("yyyy-MM-dd"),
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

                // Parse dates with specific format
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
                    CreatedAt = DateTime.UtcNow
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

        // PUT: api/projects/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] Project project)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var existingProject = await _context.Projects.FindAsync(id);

            if (existingProject == null || existingProject.CompanyId != currentUser.CompanyId)
                return NotFound();

            existingProject.Name = project.Name;
            existingProject.Description = project.Description;
            existingProject.StartDate = project.StartDate;
            existingProject.Deadline = project.Deadline;
            existingProject.Status = project.Status;
            existingProject.Priority = project.Priority;
            existingProject.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
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

                // Update project details
                project.Name = projectDto.Name;
                project.Description = projectDto.Description;
                project.StartDate = DateTime.Parse(projectDto.StartDate);
                project.Deadline = DateTime.Parse(projectDto.Deadline);
                project.Status = projectDto.Status;
                project.Priority = projectDto.Priority;
                project.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
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

                projectMember.Role = roleDto.Role;
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

                _context.ProjectMembers.Remove(projectMember);
                await _context.SaveChangesAsync();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public class ProjectCreateDto
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
            
            public List<string> TeamMemberIds { get; set; } = new List<string>();
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
                };
                _context.TaskAssignments.Add(assignment);

                project.TotalTasks++;

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
                var assignedUser = await _context.Users
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
                    AssignedUsers = new[] { assignedUser }
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
                
                // Update task details
                task.TaskTitle = taskDto.TaskTitle;
                task.Description = taskDto.Description;
                task.DueDate = DateTime.Parse(taskDto.DueDate);
                task.Priority = taskDto.Priority;
                task.Status = taskDto.Status;

                // Update task assignments
                _context.TaskAssignments.RemoveRange(task.Assignments);
                foreach (var userId in taskDto.AssignedUserIds)
                {
                    task.Assignments.Add(new TaskAssignment
                    {
                        TaskId = task.Id,
                        UserId = userId
                    });

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
                    }
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
                }
                else if (oldStatus == "Completed" && statusDto.Status != "Completed")
                {
                    task.Project.CompletedTasks--;
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
    }
}