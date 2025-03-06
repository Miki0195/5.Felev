using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Managly.Data;
using Managly.Models;

namespace Managly.Controllers
{
    [Authorize]
    [Route("api/attendance")]
    [ApiController]
    public class ClockInController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ClockInController(ApplicationDbContext context, UserManager<User> userManager)
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
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { success = false, error = "User not found" });

                // Check if already clocked in
                var existingAttendance = await _context.Attendances
                    .Where(a => a.UserId == user.Id && a.CheckOutTime == null)
                    .FirstOrDefaultAsync();

                if (existingAttendance != null)
                {
                    return BadRequest(new { 
                        success = false, 
                        error = "You are already clocked in" 
                    });
                }

                var now = DateTime.UtcNow;
                var attendance = new Attendance
                {
                    UserId = user.Id,
                    CheckInTime = now
                };

                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    checkInTime = attendance.CheckInTime 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    error = "An error occurred while clocking in",
                    details = ex.Message 
                });
            }
        }

        [HttpPost("clock-out")]
        public async Task<IActionResult> ClockOut()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { success = false, error = "User not found" });

                var attendance = await _context.Attendances
                    .Where(a => a.UserId == user.Id && a.CheckOutTime == null)
                    .OrderByDescending(a => a.CheckInTime)
                    .FirstOrDefaultAsync();

                if (attendance == null)
                {
                    return BadRequest(new { 
                        success = false, 
                        error = "No active session found" 
                    });
                }

                attendance.CheckOutTime = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    checkOutTime = attendance.CheckOutTime,
                    duration = (attendance.CheckOutTime - attendance.CheckInTime).Value.TotalHours
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    error = "An error occurred while clocking out",
                    details = ex.Message 
                });
            }
        }

        [HttpGet("current-session")]
        public async Task<IActionResult> GetCurrentSession()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized(new { success = false, error = "User not found" });

                var attendance = await _context.Attendances
                    .Where(a => a.UserId == user.Id && a.CheckOutTime == null)
                    .OrderByDescending(a => a.CheckInTime)
                    .FirstOrDefaultAsync();

                if (attendance == null)
                {
                    return Ok(new { active = false });
                }

                return Ok(new
                {
                    active = true,
                    checkInTime = attendance.CheckInTime,
                    elapsedTime = (DateTime.UtcNow - attendance.CheckInTime).TotalSeconds
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    error = "An error occurred while checking session status",
                    details = ex.Message 
                });
            }
        }

        [HttpGet("work-history")]
        public async Task<IActionResult> GetWorkHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var history = await _context.Attendances
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.CheckInTime)
                .Take(5)
                .ToListAsync();

            if (!history.Any()) return Ok(new { message = "No records found." });

            return Ok(history.Select(a => new
            {
                checkInTime = a.CheckInTime,
                checkOutTime = a.CheckOutTime,
                duration = a.CheckOutTime.HasValue
                    ? $"{(int)((a.CheckOutTime.Value - a.CheckInTime).TotalHours):D2}:" +
                      $"{(int)((a.CheckOutTime.Value - a.CheckInTime).TotalMinutes % 60):D2}:" +
                      $"{(int)((a.CheckOutTime.Value - a.CheckInTime).TotalSeconds % 60):D2}"
                    : "Ongoing"
            }));
        }

        [HttpGet("weekly-hours")]
        public async Task<IActionResult> GetWeeklyHours()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                var startOfWeek = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek);

                // Fetch all records first
                var attendanceRecords = await _context.Attendances
                    .Where(a => a.UserId == user.Id && a.CheckInTime >= startOfWeek && a.CheckOutTime.HasValue)
                    .ToListAsync();

                // Compute total minutes manually
                double totalMinutes = attendanceRecords
                    .Sum(a => (a.CheckOutTime.Value - a.CheckInTime).TotalMinutes);

                return Ok(new
                {
                    totalHours = Math.Floor(totalMinutes / 60),
                    totalMinutes = Math.Round(totalMinutes % 60)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR in GetWeeklyHours(): {ex.Message}");
                return StatusCode(500, new { error = "An error occurred while fetching weekly hours.", details = ex.Message });
            }
        }
    }
}
