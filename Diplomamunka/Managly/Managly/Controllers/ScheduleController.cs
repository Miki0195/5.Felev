using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Managly.Data;
using Managly.Models;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Managly.Controllers
{
    // Add or update DTO classes
    public class ScheduleDTO
    {
        public string UserId { get; set; }
        public DateTime ShiftDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Comment { get; set; }
    }

    public class ScheduleUpdateDTO
    {
        public string ShiftDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Comment { get; set; }
    }

    public class AvailabilityCheckDTO
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public List<string> SelectedDays { get; set; }
    }

    public class LeaveRequestDTO
    {
        public string UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Type { get; set; } // "Vacation", "Sick", "Personal", etc.
        public string Reason { get; set; }
    }

    [Authorize]
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public ScheduleController(ApplicationDbContext context, UserManager<User> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            var adminUser = await _userManager.GetUserAsync(User);
            if (adminUser == null) return Unauthorized();

            var workers = await _userManager.Users
                .Where(u => u.CompanyId == adminUser.CompanyId)
                .ToListAsync();

            return View(workers);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [Route("api/schedule/{workerId}")]
        public async Task<IActionResult> GetSchedule(string workerId)
        {
            var schedule = await _context.Schedules
                .Where(s => s.UserId == workerId)
                .Select(s => new
                {
                    id = s.Id,
                    title = $"{s.StartTime.ToString(@"hh\:mm")} - {s.EndTime.ToString(@"hh\:mm")}" +
                    (!string.IsNullOrEmpty(s.Comment) ? $"\n{s.Comment}" : ""),
                    start = s.ShiftDate.ToString("yyyy-MM-dd"),
                    allDay = true
                })
                .ToListAsync();

            return Ok(schedule); 
        }


        [Authorize(Roles = "Admin")]
        [HttpPost]
        [Route("api/schedule")]
        public async Task<IActionResult> SaveSchedule([FromBody] ScheduleDTO model)
        {
            if (string.IsNullOrEmpty(model.UserId) || model.ShiftDate == default || string.IsNullOrEmpty(model.StartTime) || string.IsNullOrEmpty(model.EndTime))
            {
                return BadRequest("Invalid data. Please provide a user, date, start time, and end time.");
            }

            if (!TimeSpan.TryParse(model.StartTime, out TimeSpan parsedStartTime) ||
                !TimeSpan.TryParse(model.EndTime, out TimeSpan parsedEndTime))
            {
                return BadRequest("Invalid time format.");
            }

            var newSchedule = new Schedule
            {
                UserId = model.UserId,
                ShiftDate = model.ShiftDate.Date,
                StartTime = parsedStartTime,
                EndTime = parsedEndTime,
                Comment = model.Comment 
            };

            _context.Schedules.Add(newSchedule);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }


        [Authorize(Roles = "Admin")]
        [HttpDelete]
        [Route("api/schedule/delete/{id}")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                return NotFound(new { success = false, message = "Shift not found" });
            }

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Shift deleted successfully" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut]
        [Route("api/schedule/update/{id}")]
        public async Task<IActionResult> UpdateSchedule(int id, [FromBody] ScheduleUpdateDTO model)
        {
            try
            {
                // Log the received data for debugging
                Console.WriteLine($"Updating schedule {id} with data: {JsonSerializer.Serialize(model)}");
                
                var schedule = await _context.Schedules.FindAsync(id);
                if (schedule == null)
                {
                    return NotFound(new { success = false, message = "Shift not found" });
                }

                // Parse the shift date if provided
                if (!string.IsNullOrEmpty(model.ShiftDate))
                {
                    if (DateTime.TryParse(model.ShiftDate, out DateTime parsedDate))
                    {
                        schedule.ShiftDate = parsedDate.Date;
                        
                        // Check if the date is in the past
                        if (parsedDate.Date < DateTime.Today)
                        {
                            return BadRequest(new { success = false, message = "Cannot move shift to a past date." });
                        }
                    }
                    else
                    {
                        return BadRequest(new { success = false, message = $"Invalid date format: {model.ShiftDate}" });
                    }
                }

                if (string.IsNullOrEmpty(model.StartTime) || string.IsNullOrEmpty(model.EndTime))
                {
                    return BadRequest(new { success = false, message = "Start time and end time are required." });
                }

                if (!TimeSpan.TryParse(model.StartTime, out TimeSpan parsedStartTime) ||
                    !TimeSpan.TryParse(model.EndTime, out TimeSpan parsedEndTime))
                {
                    return BadRequest(new { success = false, message = $"Invalid time format. Start: {model.StartTime}, End: {model.EndTime}" });
                }

                schedule.StartTime = parsedStartTime;
                schedule.EndTime = parsedEndTime;
                schedule.Comment = model.Comment ?? schedule.Comment; // Keep existing comment if new one is null

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Shift updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating schedule: {ex.Message}");
                return StatusCode(500, new { success = false, message = $"Server error: {ex.Message}" });
            }
        }

        [Authorize] 
        public async Task<IActionResult> ViewSchedule()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized(); 
            }

            return View();
        }

        [Authorize]
        [HttpGet]
        [Route("api/schedule/myschedule")]
        public async Task<IActionResult> GetMySchedule()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var schedule = await _context.Schedules
                .Where(s => s.UserId == userId)
                .Select(s => new
                {
                    id = s.Id,
                    title = $"{s.StartTime.ToString(@"hh\:mm")} - {s.EndTime.ToString(@"hh\:mm")}" +
                    (!string.IsNullOrEmpty(s.Comment) ? $"\n{s.Comment}" : ""),
                    start = s.ShiftDate.ToString("yyyy-MM-dd"),
                    allDay = true
                })
                .ToListAsync();

            return Ok(schedule);
        }


        [Authorize]
        [HttpPost]
        [Route("api/leave/request")]
        public async Task<IActionResult> RequestLeave([FromBody] List<Leave> leaveRequests)
        {
            try
            {
                if (leaveRequests == null || !leaveRequests.Any())
                {
                    return BadRequest(new { success = false, message = "No leave dates provided." });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { success = false, message = "User not authenticated." });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found." });
                }

                // Calculate working days (excluding weekends)
                int workingDaysCount = 0;
                foreach (var leave in leaveRequests)
                {
                    if (leave.LeaveDate.DayOfWeek != DayOfWeek.Saturday && leave.LeaveDate.DayOfWeek != DayOfWeek.Sunday)
                    {
                        workingDaysCount++;
                    }
                }

                // Check if user has enough vacation days
                int remainingVacationDays = user.TotalVacationDays - user.UsedVacationDays;
                if (workingDaysCount > remainingVacationDays)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = $"You don't have enough vacation days. You have {remainingVacationDays} days available, but you're requesting {workingDaysCount} working days." 
                    });
                }

                foreach (var leave in leaveRequests)
                {
                    leave.UserId = userId;
                    leave.Status = "Pending";
                    
                    if (string.IsNullOrEmpty(leave.MedicalProof))
                    {
                        leave.MedicalProof = "";
                    }
                    
                    _context.Leaves.Add(leave);
                }

                await _context.SaveChangesAsync();

                var companyId = user.CompanyId;
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                var adminEmails = adminUsers
                    .Where(admin => admin.CompanyId == companyId)
                    .Select(admin => admin.Email)
                    .ToList();

                if (adminEmails.Count > 0)
                {
                    string subject = "New Leave Request Submitted";
                    string message = $"User {user.Name} {user.LastName} has requested leave on multiple days.";

                    var emailService = new EmailService(_configuration);
                    await emailService.SendEmailsWithSmtpAsync(adminEmails, subject, message, "no-reply@yourcompany.com", "Managly System");
                }

                return Ok(new { success = true, message = "Leave request submitted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error requesting leave: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred while submitting the leave request." });
            }
        }
        
        [Authorize]
        [HttpGet]
        [Route("api/leave/myleaves")]
        public async Task<IActionResult> GetUserLeaves()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var leaves = await _context.Leaves
                .Where(l => l.UserId == userId && l.Status != "Rejected") 
                .Select(l => new
                {
                    id = l.Id,
                    title = l.Status == "Pending" ? "Vacation (Pending)" : "Vacation",
                    start = l.LeaveDate.ToString("yyyy-MM-dd"),
                    allDay = true,
                    color = l.Status == "Pending" ? "orange" : "green"
                })
                .ToListAsync();

            return Ok(leaves);
        }


        [Authorize(Roles = "Admin")]
        [HttpGet]
        [Route("api/leave/{workerId}")]
        public async Task<IActionResult> GetUserLeaveRequests(string workerId)
        {
            var leaves = await _context.Leaves
                .Where(l => l.UserId == workerId && (l.Status == "Pending" || l.Status == "Approved"))
                .Select(l => new
                {
                    id = l.Id,
                    start = l.LeaveDate.ToString("yyyy-MM-dd"),
                    title = l.Status == "Pending" ? "Vacation (Pending)" : "Vacation (Approved)",
                    allDay = true,
                    color = l.Status == "Pending" ? "orange" : "green" 
                })
                .ToListAsync();

            return Ok(leaves);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut]
        [Route("api/leave/{id}/status")]
        public async Task<IActionResult> UpdateLeaveStatus(int id, [FromBody] string status)
        {
            try
            {
                var leave = await _context.Leaves.FindAsync(id);
                if (leave == null)
                {
                    return NotFound(new { success = false, message = "Leave request not found." });
                }

                // Check if this is a status change from pending to approved or vice versa
                bool wasApproved = leave.Status == "Approved";
                bool isNowApproved = status == "Approved";

                // Update the leave status
                leave.Status = status;
                
                // Get the user who requested the leave
                var user = await _userManager.FindByIdAsync(leave.UserId);
                if (user != null)
                {
                    // If approving a leave request that wasn't approved before
                    if (isNowApproved && !wasApproved)
                    {
                        // Check if the leave date is a weekday (not Saturday or Sunday)
                        if (leave.LeaveDate.DayOfWeek != DayOfWeek.Saturday && leave.LeaveDate.DayOfWeek != DayOfWeek.Sunday)
                        {
                            // Increment used vacation days
                            user.UsedVacationDays += 1;
                            await _userManager.UpdateAsync(user);
                        }
                    }
                    // If rejecting a leave request that was previously approved
                    else if (!isNowApproved && wasApproved)
                    {
                        // Check if the leave date is a weekday (not Saturday or Sunday)
                        if (leave.LeaveDate.DayOfWeek != DayOfWeek.Saturday && leave.LeaveDate.DayOfWeek != DayOfWeek.Sunday)
                        {
                            // Decrement used vacation days, but don't go below zero
                            user.UsedVacationDays = Math.Max(0, user.UsedVacationDays - 1);
                            await _userManager.UpdateAsync(user);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Leave status updated successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating leave status: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred while updating the leave status." });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut]
        [Route("api/leave/{id}/move")]
        public async Task<IActionResult> MoveVacation(int id, [FromBody] string newDate)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave == null)
            {
                return NotFound();
            }

            if (!DateTime.TryParse(newDate, out DateTime parsedDate))
            {
                return BadRequest(new { success = false, message = "Invalid date format" });
            }

            leave.LeaveDate = parsedDate;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete]
        [Route("api/leave/delete/{id}")]
        public async Task<IActionResult> DeleteLeaveRequest(int id)
        {
            try
            {
                var leave = await _context.Leaves.FindAsync(id);
                if (leave == null)
                {
                    return NotFound(new { success = false, message = "Vacation request not found." });
                }

                // If the leave was approved, restore the vacation day
                if (leave.Status == "Approved")
                {
                    var user = await _userManager.FindByIdAsync(leave.UserId);
                    if (user != null)
                    {
                        // Check if the leave date is a weekday (not Saturday or Sunday)
                        if (leave.LeaveDate.DayOfWeek != DayOfWeek.Saturday && leave.LeaveDate.DayOfWeek != DayOfWeek.Sunday)
                        {
                            // Decrement used vacation days, but don't go below zero
                            user.UsedVacationDays = Math.Max(0, user.UsedVacationDays - 1);
                            await _userManager.UpdateAsync(user);
                        }
                    }
                }

                _context.Leaves.Remove(leave);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Vacation request deleted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting leave request: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred while deleting the vacation request." });
            }
        }

        [Authorize(Roles = "Admin")]
        public IActionResult MergedSchedule()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [Route("api/schedule/all")]
        public async Task<IActionResult> GetAllSchedules()
        {
            try
            {
                var schedules = await _context.Schedules
                    .Include(s => s.User)
                    .Select(s => new
                    {
                        id = s.Id,
                        title = $"{s.StartTime.ToString(@"hh\:mm")} - {s.EndTime.ToString(@"hh\:mm")}" +
                                (!string.IsNullOrEmpty(s.Comment) ? $"\n{s.Comment}" : ""),
                        start = s.ShiftDate.Date.ToString("yyyy-MM-dd"), // ✅ Ensure correct date format
                        allDay = true,
                        workerName = s.User != null ? s.User.Name + " " + s.User.LastName : "Unknown Worker", // ✅ Prevent null issues
                        color = GetUserColor(s.User.Id ?? "default"), // ✅ Safe null handling
                        status = s.Status ?? "Pending" // ✅ Ensure status is not null
                    })
                    .ToListAsync();

                return Ok(schedules);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching schedules: {ex.Message}");
                return StatusCode(500, new { error = ex.Message }); // ✅ Return a readable error
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [Route("api/schedule/search-workers")]
        public async Task<IActionResult> SearchWorkers(string query)
        {
            var adminUser = await _userManager.GetUserAsync(User);
            if (adminUser == null || adminUser.CompanyId == null) 
                return Unauthorized();

            var workers = await _userManager.Users
                .Where(u => u.CompanyId == adminUser.CompanyId &&
                           (u.Name.Contains(query) || 
                            u.LastName.Contains(query) || 
                            (u.Name + " " + u.LastName).Contains(query)))
                .Select(u => new { 
                    id = u.Id, 
                    name = u.Name, 
                    lastName = u.LastName 
                })
                .ToListAsync();

            return Ok(workers);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [Route("api/schedule/bulk")]
        public async Task<IActionResult> SaveBulkSchedule([FromBody] List<ScheduleDTO> shifts)
        {
            if (!shifts.Any())
                return BadRequest(new { success = false, message = "No shifts provided." });

            try
            {
                var newSchedules = shifts.Select(shift => new Schedule
                {
                    UserId = shift.UserId,
                    ShiftDate = shift.ShiftDate,
                    StartTime = TimeSpan.Parse(shift.StartTime),
                    EndTime = TimeSpan.Parse(shift.EndTime),
                    Comment = shift.Comment
                }).ToList();

                await _context.Schedules.AddRangeAsync(newSchedules);
                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [Route("api/schedule/{userId}/check-availability")]
        public async Task<IActionResult> CheckAvailability(string userId, [FromBody] AvailabilityCheckDTO data)
        {
            var startDate = DateTime.Parse(data.StartDate);
            var endDate = DateTime.Parse(data.EndDate);
            var selectedDays = data.SelectedDays;

            // Check for existing shifts
            var existingShifts = await _context.Schedules
                .Where(s => s.UserId == userId && 
                            s.ShiftDate >= startDate && 
                            s.ShiftDate <= endDate)
                .ToListAsync();

            // Check for existing leaves (vacations)
            var existingLeaves = await _context.Leaves
                .Where(l => l.UserId == userId && 
                            l.LeaveDate >= startDate && 
                            l.LeaveDate <= endDate &&
                            (l.Status == "Pending" || l.Status == "Approved"))
                .ToListAsync();

            // Check each day in the range
            var currentDate = startDate;
            while (currentDate <= endDate)
            {
                var dayName = currentDate.ToString("dddd").ToLower();
                if (selectedDays.Contains(dayName))
                {
                    if (existingShifts.Any(s => s.ShiftDate.Date == currentDate.Date) ||
                        existingLeaves.Any(l => l.LeaveDate.Date == currentDate.Date))
                    {
                        return Ok(new { available = false });
                    }
                }
                currentDate = currentDate.AddDays(1);
            }

            return Ok(new { available = true });
        }

        private static string GetUserColor(string userId)
        {
            var colors = new List<string> { "#3498db", "#e74c3c", "#2ecc71", "#f39c12", "#9b59b6", "#1abc9c" };
            int index = Math.Abs(userId.GetHashCode()) % colors.Count;
            return colors[index];
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserSchedule(string userId)
        {
            try
            {
                var shifts = await _context.Schedules
                    .Where(s => s.UserId == userId)
                    .Select(s => new
                    {
                        id = s.Id,
                        title = $"{s.StartTime} - {s.EndTime}",
                        start = s.ShiftDate.ToString("yyyy-MM-dd"),
                        startTime = s.StartTime,
                        endTime = s.EndTime,
                        comment = s.Comment,
                        allDay = true
                    })
                    .ToListAsync();

                return Ok(shifts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/schedule/filtered")]
        public async Task<IActionResult> GetFilteredSchedules([FromQuery] string workers, [FromQuery] string projects)
        {
            var workerIds = !string.IsNullOrEmpty(workers) 
                ? workers.Split(',').ToList() 
                : null;
            
            var projectIds = !string.IsNullOrEmpty(projects) 
                ? projects.Split(',').Select(int.Parse).ToList() 
                : null;

            var query = _context.Schedules.AsQueryable();

            if (workerIds != null && workerIds.Any())
            {
                query = query.Where(s => workerIds.Contains(s.UserId));
            }

            if (projectIds != null && projectIds.Any())
            {
                // Get all users who are members of the selected projects
                var projectMemberIds = await _context.ProjectMembers
                    .Where(pm => projectIds.Contains(pm.ProjectId))
                    .Select(pm => pm.UserId)
                    .Distinct()
                    .ToListAsync();

                // Filter schedules to only show those from project members
                query = query.Where(s => projectMemberIds.Contains(s.UserId));
            }

            var schedules = await query
                .Include(s => s.User)
                .Select(s => new
                {
                    id = s.Id,
                    title = $"{s.StartTime.ToString(@"hh\:mm")} - {s.EndTime.ToString(@"hh\:mm")}" +
                            (!string.IsNullOrEmpty(s.Comment) ? $"\n{s.Comment}" : ""),
                    start = s.ShiftDate.ToString("yyyy-MM-dd"),
                    allDay = true,
                    workerName = s.User.Name + " " + s.User.LastName,
                    workerId = s.UserId,
                    color = GetUserColor(s.UserId)
                })
                .ToListAsync();

            return Ok(schedules);
        }

        [HttpGet]
        [Route("api/schedule/myvacationinfo")]
        public async Task<IActionResult> GetCurrentUserVacationInfo()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated." });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found." });
                }

                // Check if we need to reset vacation days for the new year
                int currentYear = DateTime.Now.Year;
                if (user.VacationYear < currentYear)
                {
                    user.VacationYear = currentYear;
                    user.UsedVacationDays = 0;
                    await _userManager.UpdateAsync(user);
                }

                return Ok(new
                {
                    id = user.Id,
                    name = user.Name,
                    lastName = user.LastName,
                    email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    totalVacationDays = user.TotalVacationDays,
                    usedVacationDays = user.UsedVacationDays,
                    remainingVacationDays = user.RemainingVacationDays,
                    vacationYear = user.VacationYear
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user vacation info: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred while getting user information." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitLeaveRequest([FromBody] LeaveRequestDTO request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.UserId) || request.StartDate == default || request.EndDate == default)
                {
                    return BadRequest(new { success = false, message = "Invalid request data." });
                }

                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found." });
                }

                // Calculate the number of working days in the leave request
                int workingDays = CountWorkingDays(request.StartDate, request.EndDate);

                // Check if user has enough vacation days remaining
                if (request.Type == "Vacation" && user.RemainingVacationDays < workingDays)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = $"Not enough vacation days remaining. You have {user.RemainingVacationDays} days available, but requested {workingDays} days." 
                    });
                }

                // Create leave entries for each day in the date range
                var currentDate = request.StartDate.Date;
                var endDate = request.EndDate.Date;
                var schedules = new List<Schedule>();

                while (currentDate <= endDate)
                {
                    // Skip weekends if it's a vacation request
                    if (request.Type == "Vacation" && (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday))
                    {
                        currentDate = currentDate.AddDays(1);
                        continue;
                    }

                    var schedule = new Schedule
                    {
                        UserId = request.UserId,
                        ShiftDate = currentDate,
                        StartTime = new TimeSpan(9, 0, 0), // Default work hours
                        EndTime = new TimeSpan(17, 0, 0),
                        Comment = request.Reason,
                        IsHolidayRequest = true,
                        Status = "Pending"
                    };

                    schedules.Add(schedule);
                    currentDate = currentDate.AddDays(1);
                }

                await _context.Schedules.AddRangeAsync(schedules);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Leave request submitted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while submitting the leave request." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApproveLeaveRequest(int requestId)
        {
            try
            {
                var schedule = await _context.Schedules.FindAsync(requestId);
                if (schedule == null || !schedule.IsHolidayRequest)
                {
                    return NotFound(new { success = false, message = "Leave request not found." });
                }

                var user = await _userManager.FindByIdAsync(schedule.UserId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found." });
                }

                // Update schedule status
                schedule.Status = "Approved";

                // If it's a vacation day (not weekend), increment used vacation days
                if (schedule.ShiftDate.DayOfWeek != DayOfWeek.Saturday && 
                    schedule.ShiftDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    user.UsedVacationDays += 1;
                    await _userManager.UpdateAsync(user);
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Leave request approved successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while approving the leave request." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectLeaveRequest(int requestId)
        {
            try
            {
                var schedule = await _context.Schedules.FindAsync(requestId);
                if (schedule == null || !schedule.IsHolidayRequest)
                {
                    return NotFound(new { success = false, message = "Leave request not found." });
                }

                // Update schedule status
                schedule.Status = "Rejected";
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Leave request rejected successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while rejecting the leave request." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelLeaveRequest(int requestId)
        {
            try
            {
                var schedule = await _context.Schedules.FindAsync(requestId);
                if (schedule == null || !schedule.IsHolidayRequest)
                {
                    return NotFound(new { success = false, message = "Leave request not found." });
                }

                // If the request was approved, restore the vacation day
                if (schedule.Status == "Approved")
                {
                    var user = await _userManager.FindByIdAsync(schedule.UserId);
                    if (user != null && schedule.ShiftDate.DayOfWeek != DayOfWeek.Saturday && 
                        schedule.ShiftDate.DayOfWeek != DayOfWeek.Sunday)
                    {
                        user.UsedVacationDays = Math.Max(0, user.UsedVacationDays - 1);
                        await _userManager.UpdateAsync(user);
                    }
                }

                // Remove the schedule entry
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Leave request cancelled successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while cancelling the leave request." });
            }
        }

        /// <summary>
        /// Counts the number of working days (Monday to Friday) between two dates
        /// </summary>
        private int CountWorkingDays(DateTime startDate, DateTime endDate)
        {
            int workingDays = 0;
            DateTime currentDate = startDate.Date;

            while (currentDate <= endDate.Date)
            {
                // Check if the current day is a weekday (Monday to Friday)
                if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
                currentDate = currentDate.AddDays(1);
            }

            return workingDays;
        }
    }
}
