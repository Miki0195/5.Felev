using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Managly.Data;
using Managly.Models;
using Managly.Models.DTOs.ClockIn;
using Microsoft.Extensions.Logging;

namespace Managly.Controllers
{
    [Authorize]
    [Route("api/attendance")]
    [ApiController]
    public class ClockInController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ClockInController(
            ApplicationDbContext context, 
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("")]
        [Route("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("clock-in")]
        public async Task<IActionResult> ClockIn()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponseDto { Success = false, Error = "User not found" });
                }

                bool alreadyClockedIn = await _context.Attendances
                    .AnyAsync(a => a.UserId == userId && a.CheckOutTime == null);

                if (alreadyClockedIn)
                {
                    return BadRequest(new ApiResponseDto { 
                        Success = false, 
                        Error = "You are already clocked in" 
                    });
                }

                var now = DateTime.Now;
                var attendance = new Attendance
                {
                    UserId = userId,
                    CheckInTime = now
                };

                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();

                return Ok(new ClockInResponseDto { 
                    Success = true, 
                    CheckInTime = attendance.CheckInTime 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDto { 
                    Success = false, 
                    Error = "An error occurred while clocking in"
                });
            }
        }

        [HttpPost("clock-out")]
        public async Task<IActionResult> ClockOut()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponseDto { Success = false, Error = "User not found" });
                }

                var attendance = await _context.Attendances
                    .Where(a => a.UserId == userId && a.CheckOutTime == null)
                    .OrderByDescending(a => a.CheckInTime)
                    .FirstOrDefaultAsync();

                if (attendance == null)
                {
                    return BadRequest(new ApiResponseDto { 
                        Success = false, 
                        Error = "No active session found" 
                    });
                }

                var now = DateTime.Now;
                attendance.CheckOutTime = now;
                
                _context.Entry(attendance).Property(a => a.CheckOutTime).IsModified = true;
                await _context.SaveChangesAsync();

                var duration = (now - attendance.CheckInTime).TotalHours;

                return Ok(new ClockOutResponseDto { 
                    Success = true, 
                    CheckOutTime = now,
                    Duration = Math.Round(duration, 2)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDto { 
                    Success = false, 
                    Error = "An error occurred while clocking out"
                });
            }
        }

        [HttpGet("current-session")]
        public async Task<IActionResult> GetCurrentSession()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponseDto { Success = false, Error = "User not found" });
                }

                var activeSession = await _context.Attendances
                    .Where(a => a.UserId == userId && a.CheckOutTime == null)
                    .OrderByDescending(a => a.CheckInTime)
                    .Select(a => new { a.CheckInTime })
                    .FirstOrDefaultAsync();

                if (activeSession == null)
                {
                    return Ok(new SessionStatusDto { Active = false });
                }

                var elapsedTime = (DateTime.Now - activeSession.CheckInTime).TotalSeconds;

                return Ok(new SessionStatusDto
                {
                    Active = true,
                    CheckInTime = activeSession.CheckInTime,
                    ElapsedTime = Math.Round(elapsedTime, 1)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDto { 
                    Success = false, 
                    Error = "An error occurred while checking session status"
                });
            }
        }

        [HttpGet("work-history")]
        public async Task<IActionResult> GetWorkHistory()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponseDto { Success = false, Error = "User not found" });
                }

                var history = await _context.Attendances
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.CheckInTime)
                    .Take(5)
                    .Select(a => new { 
                        a.CheckInTime, 
                        a.CheckOutTime
                    })
                    .ToListAsync();

                if (!history.Any())
                {
                    return Ok(new { message = "No records found." });
                }

                var historyDtos = new List<WorkHistoryEntryDto>();
                
                foreach (var entry in history)
                {
                    string duration;
                    if (entry.CheckOutTime.HasValue)
                    {
                        var diff = entry.CheckOutTime.Value - entry.CheckInTime;
                        duration = $"{(int)diff.TotalHours:D2}:{diff.Minutes:D2}:{diff.Seconds:D2}";
                    }
                    else
                    {
                        duration = "Ongoing";
                    }
                    
                    historyDtos.Add(new WorkHistoryEntryDto
                    {
                        CheckInTime = entry.CheckInTime,
                        CheckOutTime = entry.CheckOutTime,
                        Duration = duration
                    });
                }

                return Ok(historyDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDto { 
                    Success = false, 
                    Error = "An error occurred while fetching work history"
                });
            }
        }

        [HttpGet("weekly-hours")]
        public async Task<IActionResult> GetWeeklyHours()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponseDto { Success = false, Error = "User not found" });
                }

                var startOfWeek = DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek);
                
                var attendanceRecords = await _context.Attendances
                    .Where(a => a.UserId == userId && 
                           a.CheckInTime >= startOfWeek && 
                           a.CheckOutTime != null)
                    .ToListAsync();

                double totalMinutes = 0;
                foreach (var record in attendanceRecords)
                {
                    if (record.CheckOutTime.HasValue)
                    {
                        var duration = record.CheckOutTime.Value - record.CheckInTime;
                        totalMinutes += duration.TotalMinutes;
                    }
                }
                
                var hours = Math.Floor(totalMinutes / 60);
                var minutes = Math.Round(totalMinutes % 60);
                
                return Ok(new WeeklyHoursDto
                {
                    TotalHours = hours,
                    TotalMinutes = minutes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDto { 
                    Success = false, 
                    Error = "An error occurred while calculating weekly hours"
                });
            }
        }

        [HttpGet("user/is-admin")]
        public async Task<IActionResult> IsAdmin()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { isAdmin = false });

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin") || 
                          await _userManager.IsInRoleAsync(user, "Manager");

            return Ok(new { isAdmin });
        }

        [HttpGet("team-overview")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetTeamOverview()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { success = false, error = "User not found" });

                var companyId = user.CompanyId;
                if (!companyId.HasValue)
                    return BadRequest(new { success = false, error = "Company not found" });

                var startOfWeek = DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek);

                var companyUsers = await _context.Users
                    .Where(u => u.CompanyId == companyId)
                    .ToListAsync();

                var teamMembers = new List<object>();

                foreach (var member in companyUsers)
                {
                    var activeSession = await _context.Attendances
                        .Where(a => a.UserId == member.Id && a.CheckOutTime == null)
                        .OrderByDescending(a => a.CheckInTime)
                        .FirstOrDefaultAsync();

                    var weeklyHours = await CalculateWeeklyHoursAsync(member.Id, startOfWeek);

                    var roles = await _userManager.GetRolesAsync(member);
                    var position = roles.FirstOrDefault() ?? "Employee";

                    teamMembers.Add(new
                    {
                        UserId = member.Id,
                        Name = $"{member.Name} {member.LastName}",
                        Position = position,
                        ProfilePicture = member.ProfilePicturePath,
                        IsActive = activeSession != null,
                        CurrentSessionStart = activeSession?.CheckInTime,
                        WeeklyHours = Math.Round(weeklyHours, 1)
                    });
                }

                int activeCount = teamMembers.Count(m => (bool)(m as dynamic).IsActive);
                double totalWeeklyHours = teamMembers.Sum(m => (double)(m as dynamic).WeeklyHours);
                double avgWeeklyHours = teamMembers.Count > 0 ? Math.Round(totalWeeklyHours / teamMembers.Count, 1) : 0;
                double totalOvertimeHours = teamMembers.Sum(m => Math.Max(0, (double)(m as dynamic).WeeklyHours - 40));

                var stats = new
                {
                    ActiveCount = activeCount,
                    WeeklyAverageHours = avgWeeklyHours,
                    TotalOvertimeHours = Math.Round(totalOvertimeHours, 1)
                };

                return Ok(new
                {
                    TeamMembers = teamMembers,
                    Stats = stats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred while fetching team overview" });
            }
        }

        [HttpGet("latest-record/{userId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetLatestAttendanceRecord(string userId)
        {
            try
            {
                var requester = await _userManager.GetUserAsync(User);
                if (requester == null)
                    return Unauthorized(new { success = false, error = "Not authorized" });

                var targetUser = await _context.Users.FindAsync(userId);
                if (targetUser == null || targetUser.CompanyId != requester.CompanyId)
                    return NotFound(new { success = false, error = "User not found" });

                var latestRecord = await _context.Attendances
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.CheckInTime)
                    .Take(5)
                    .Select(a => new
                    {
                        Id = a.Id,
                        CheckInTime = a.CheckInTime,
                        CheckOutTime = a.CheckOutTime
                    })
                    .FirstOrDefaultAsync();

                if (latestRecord == null)
                    return Ok(new { success = true, message = "No records found" });

                return Ok(latestRecord);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred" });
            }
        }

        [HttpPost("update-time")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateAttendanceTime([FromBody] UpdateTimeRequestDto request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.UserId))
                    return BadRequest(new { success = false, error = "Invalid request" });

                var requester = await _userManager.GetUserAsync(User);
                if (requester == null)
                    return Unauthorized(new { success = false, error = "Not authorized" });

                var targetUser = await _context.Users.FindAsync(request.UserId);
                if (targetUser == null || targetUser.CompanyId != requester.CompanyId)
                    return NotFound(new { success = false, error = "User not found" });

                Attendance record;
                if (!string.IsNullOrEmpty(request.RecordId) && int.TryParse(request.RecordId, out int recordId))
                {
                    record = await _context.Attendances.FindAsync(recordId);
                    if (record == null || record.UserId != request.UserId)
                        return NotFound(new { success = false, error = "Record not found" });
                }
                else
                {
                    record = new Attendance
                    {
                        UserId = request.UserId
                    };
                    _context.Attendances.Add(record);
                }

                if (DateTime.TryParse(request.CheckInTime, out DateTime checkInTime))
                {
                    record.CheckInTime = checkInTime;
                }
                else
                {
                    return BadRequest(new { success = false, error = "Invalid check-in time" });
                }

                if (!string.IsNullOrEmpty(request.CheckOutTime) && 
                    DateTime.TryParse(request.CheckOutTime, out DateTime checkOutTime))
                {
                    record.CheckOutTime = checkOutTime;
                }
                else
                {
                    record.CheckOutTime = null;
                }

                var auditLog = new AttendanceAuditLog
                {
                    AttendanceId = record.Id,
                    ModifiedByUserId = requester.Id,
                    ModificationTime = DateTime.Now,
                    Notes = request.AdminNotes ?? "Time edited by administrator"
                };
                _context.AttendanceAuditLogs.Add(auditLog);

                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred" });
            }
        }

        [HttpGet("employee-details/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetEmployeeDetails(string userId)
        {
            try
            {
                var requester = await _userManager.GetUserAsync(User);
                if (requester == null)
                {
                    return Unauthorized(new { success = false, error = "Not authorized" });
                }

                var employee = await _context.Users
                    .Where(u => u.Id == userId && u.CompanyId == requester.CompanyId)
                    .Select(u => new
                    {
                        Id = u.Id,
                        Name = u.Name,
                        LastName = u.LastName,
                        FullName = $"{u.Name} {u.LastName}",
                        Email = u.Email,
                        ProfilePicture = u.ProfilePicturePath
                    })
                    .FirstOrDefaultAsync();

                if (employee == null)
                {
                    return NotFound(new { success = false, error = "Employee not found" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, error = "User not found" });
                }
                
                var roles = await _userManager.GetRolesAsync(user);
                var position = roles.FirstOrDefault() ?? "Employee";

                var activeSession = await _context.Attendances
                    .Where(a => a.UserId == userId && a.CheckOutTime == null)
                    .OrderByDescending(a => a.CheckInTime)
                    .Select(a => new
                    {
                        CheckInTime = a.CheckInTime
                    })
                    .FirstOrDefaultAsync();

                var fromDate = DateTime.Now.Date.AddDays(-14);
                
                var attendanceHistory = await _context.Attendances
                    .Where(a => a.UserId == userId && a.CheckInTime >= fromDate)
                    .OrderByDescending(a => a.CheckInTime)
                    .Take(5)
                    .Select(a => new
                    {
                        Id = a.Id,
                        Date = a.CheckInTime.Date,
                        CheckInTime = a.CheckInTime,
                        CheckOutTime = a.CheckOutTime,
                    })
                    .ToListAsync();

                var processedHistory = attendanceHistory.Select(a => new
                {
                    a.Id,
                    a.Date,
                    a.CheckInTime,
                    a.CheckOutTime,
                    Duration = a.CheckOutTime.HasValue 
                        ? (a.CheckOutTime.Value - a.CheckInTime).TotalHours 
                        : (DateTime.Now - a.CheckInTime).TotalHours
                }).ToList();

                var dailySummary = new List<object>();
                dailySummary = processedHistory
                    .GroupBy(a => a.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalHours = g.Sum(a => a.Duration),
                        Sessions = g.Count()
                    })
                    .OrderByDescending(d => d.Date)
                    .ToList<object>();

                var weeklySummary = new List<object>();
                var currentDay = DateTime.Now.Date;
                var startDate = currentDay.AddDays(-(int)currentDay.DayOfWeek);

                for (int i = 0; i < 4; i++) 
                {
                    var weekStart = startDate.AddDays(-7 * i);
                    var weekEnd = weekStart.AddDays(7).AddSeconds(-1);
                        
                    var weekHours = processedHistory
                        .Where(a => a.Date >= weekStart && a.Date <= weekEnd)
                        .Sum(a => a.Duration);
                        
                    weeklySummary.Add(new
                    {
                        WeekStart = weekStart,
                        WeekEnd = weekEnd,
                        TotalHours = Math.Round(weekHours, 1),
                        Overtime = Math.Max(0, Math.Round(weekHours - 40, 1))
                    });
                }

                var result = new
                {
                    Employee = new
                    {
                        employee.Id,
                        employee.Name,
                        employee.LastName,
                        employee.FullName,
                        employee.Email,
                        employee.ProfilePicture,
                        Position = position
                    },
                    IsActive = activeSession != null,
                    CurrentSessionStart = activeSession?.CheckInTime,
                    AttendanceHistory = processedHistory,
                    DailySummary = dailySummary,
                    WeeklySummary = weeklySummary
                };
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpGet]
        [Route("/EmployeeDetails/{userId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> EmployeeDetails(string userId)
        {
            try
            {
                var requester = await _userManager.GetUserAsync(User);
                if (requester == null)
                    return RedirectToAction("Login", "Account");
            
                bool isAdmin = await _userManager.IsInRoleAsync(requester, "Admin") || 
                               await _userManager.IsInRoleAsync(requester, "Manager");
            
                if (!isAdmin && requester.Id != userId)
                    return Forbid();
            
                ViewBag.UserId = userId;
            
                var employee = await _context.Users
                    .Where(u => u.Id == userId && u.CompanyId == requester.CompanyId)
                    .Select(u => new
                    {
                        Id = u.Id,
                        Name = u.Name,
                        LastName = u.LastName,
                        FullName = $"{u.Name} {u.LastName}",
                        Email = u.Email,
                        ProfilePicture = u.ProfilePicturePath
                    })
                    .FirstOrDefaultAsync();
            
                if (employee == null)
                    return NotFound();
            
                ViewBag.Employee = employee;
            
                return View();
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index");
            }
        }

        [HttpGet("get-record/{recordId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAttendanceRecord(int recordId)
        {
            try
            {
                var requester = await _userManager.GetUserAsync(User);
                if (requester == null)
                    return Unauthorized(new { success = false, error = "Not authorized" });
            
                var record = await _context.Attendances
                    .Where(a => a.Id == recordId)
                    .Select(a => new
                    {
                        Id = a.Id,
                        UserId = a.UserId,
                        CheckInTime = a.CheckInTime,
                        CheckOutTime = a.CheckOutTime
                    })
                    .FirstOrDefaultAsync();
            
                if (record == null)
                    return NotFound(new { success = false, error = "Record not found" });
            
                var recordUser = await _context.Users.FindAsync(record.UserId);
            
                if (recordUser == null)
                    return NotFound(new { success = false, error = "User not found" });
            
                return Ok(record);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An error occurred" });
            }
        }

        [HttpGet("employee-basic/{userId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetBasicEmployeeDetails(string userId)
        {
            try
            {
                var requester = await _userManager.GetUserAsync(User);
                if (requester == null)
                    return Unauthorized(new { success = false, error = "Not authorized" });

                var employee = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new
                    {
                        Id = u.Id,
                        Name = u.Name,
                        LastName = u.LastName,
                        Email = u.Email
                    })
                    .FirstOrDefaultAsync();

                if (employee == null)
                    return NotFound(new { success = false, error = "Employee not found" });

                return Ok(new { success = true, employee });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"An error occurred: {ex.Message}" });
            }
        }

        private async Task<double> CalculateWeeklyHoursAsync(string userId, DateTime startOfWeek)
        {
            try
            {
                var completedSessions = await _context.Attendances
                    .Where(a => a.UserId == userId && 
                           a.CheckInTime >= startOfWeek && 
                           a.CheckOutTime.HasValue)
                    .ToListAsync();

                var activeSession = await _context.Attendances
                    .Where(a => a.UserId == userId && a.CheckOutTime == null)
                    .OrderByDescending(a => a.CheckInTime)
                    .FirstOrDefaultAsync();

                double totalHours = 0;

                foreach (var session in completedSessions)
                {
                    var duration = session.CheckOutTime.Value - session.CheckInTime;
                    totalHours += duration.TotalHours;
                }

                if (activeSession != null)
                {
                    var activeDuration = DateTime.Now - activeSession.CheckInTime;
                    totalHours += activeDuration.TotalHours;
                }

                return totalHours;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
    }
}
