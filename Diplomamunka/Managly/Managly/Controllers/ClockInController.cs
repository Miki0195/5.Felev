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
using Managly.Models.DTOs;
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

                var now = DateTime.UtcNow;
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

                var now = DateTime.UtcNow;
                attendance.CheckOutTime = now;
                
                // Only update the changed property
                _context.Entry(attendance).Property(a => a.CheckOutTime).IsModified = true;
                await _context.SaveChangesAsync();

                // Calculate duration immediately without requerying
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

                // Optimized query with only necessary fields
                var activeSession = await _context.Attendances
                    .Where(a => a.UserId == userId && a.CheckOutTime == null)
                    .OrderByDescending(a => a.CheckInTime)
                    .Select(a => new { a.CheckInTime })
                    .FirstOrDefaultAsync();

                if (activeSession == null)
                {
                    return Ok(new SessionStatusDto { Active = false });
                }

                var elapsedTime = (DateTime.UtcNow - activeSession.CheckInTime).TotalSeconds;

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

                // Optimized query with projection directly to DTO-compatible format
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

                var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
                
                // Fetch raw data without trying to perform calculations in the query
                var attendanceRecords = await _context.Attendances
                    .Where(a => a.UserId == userId && 
                           a.CheckInTime >= startOfWeek && 
                           a.CheckOutTime != null)
                    .ToListAsync();

                // Manual calculation to avoid EF Core translation issues
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
                // Enhanced error logging
                Console.WriteLine($"Weekly hours error at {DateTime.UtcNow}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return StatusCode(500, new ApiResponseDto { 
                    Success = false, 
                    Error = "An error occurred while calculating weekly hours"
                });
            }
        }
    }
}
