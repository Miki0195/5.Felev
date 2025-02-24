using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Managly.Data;
using Managly.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Managly.Controllers
{
    [Authorize]
    [Route("api/notifications")]
    [ApiController]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public NotificationController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] Notification model)
        {
            if (model == null || string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.Message))
            {
                return BadRequest(new { error = "Invalid notification data." });
            }

            model.Timestamp = DateTime.UtcNow;
            _context.Notifications.Add(model);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = _userManager.GetUserId(User);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.Timestamp)
                .ToListAsync();

            return Ok(notifications);
        }

        [HttpPost("mark-as-read/{senderId}")]
        public async Task<IActionResult> MarkNotificationsAsRead(string senderId)
        {
            var userId = _userManager.GetUserId(User);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.Link.Contains($"userId={senderId}") && !n.IsRead)
                .ToListAsync();

            if (!notifications.Any())
                return Ok(new { success = false, message = "No unread notifications found" });

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost("mark-all-as-read")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {

            var userId = _userManager.GetUserId(User);

            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (!unreadNotifications.Any()) return Ok(new { success = false, message = "No unread notifications" });

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost("mark-as-read/project_{projectId}")]
        public async Task<IActionResult> MarkProjectNotificationsAsRead(int projectId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId && 
                               n.Link.Contains($"projectId={projectId}") && 
                               !n.IsRead)
                    .ToListAsync();

                if (!notifications.Any())
                    return Ok(new { success = false, message = "No unread notifications found" });

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        //[HttpPost("video-call")]
        //public async Task<IActionResult> SendVideoCallInvitation([FromBody] Notification model)
        //{
        //    if (model == null || string.IsNullOrEmpty(model.UserId))
        //        return BadRequest(new { error = "Invalid data." });

        //    model.Message = "You have been invited to a video call.";
        //    model.Timestamp = DateTime.UtcNow;
        //    _context.Notifications.Add(model);
        //    await _context.SaveChangesAsync();

        //    return Ok(new { success = true });
        //}

    }
}
