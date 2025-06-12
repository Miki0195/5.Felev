using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Managly.Data;
using Managly.Models;
using Managly.Models.DTOs.Notification;
using Managly.Models.Enums;
using System.Text.Json;
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
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.UserId) || string.IsNullOrEmpty(dto.Message))
            {
                return BadRequest(new { error = "Invalid notification data." });
            }

            var notification = new Notification
            {
                UserId = dto.UserId,
                Message = dto.Message,
                Link = dto.Link,
                Type = dto.Type,
                Timestamp = DateTime.UtcNow,
                ProjectId = dto.ProjectId,
                TaskId = dto.TaskId,
                RelatedUserId = dto.RelatedUserId,
                MetaData = dto.MetaData != null ? JsonSerializer.Serialize(dto.MetaData) : null
            };

            _context.Notifications.Add(notification);
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

            var notificationDtos = notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                Message = n.Message,
                Link = n.Link,
                Timestamp = n.Timestamp,
                Type = n.Type,
                RelatedUserId = n.RelatedUserId,
                MetaData = string.IsNullOrEmpty(n.MetaData) 
                    ? new Dictionary<string, string>()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(n.MetaData)
            }).ToList();

            var groupedNotifications = notificationDtos
                .GroupBy(n => n.Type)
                .Select(typeGroup => new NotificationGroupDto
                {
                    Type = typeGroup.Key,
                    GroupTitle = GetGroupTitle(typeGroup.Key),
                    // Group by RelatedUserId within each type
                    SenderGroups = typeGroup
                        .GroupBy(n => n.RelatedUserId)
                        .Select(senderGroup => new NotificationSenderGroupDto
                        {
                            SenderId = senderGroup.Key ?? "system",
                            SenderName = senderGroup.First().MetaData.GetValueOrDefault("senderName", "System"),
                            Notifications = senderGroup.ToList(),
                            UnreadCount = senderGroup.Count()
                        })
                        .ToList(),
                    UnreadCount = typeGroup.Count()
                })
                .OrderByDescending(g => g.SenderGroups.SelectMany(sg => sg.Notifications).Max(n => n.Timestamp))
                .ToList();

            return Ok(groupedNotifications);
        }

        [HttpPost("mark-as-read/{notificationType}")]
        public async Task<IActionResult> MarkNotificationsAsRead(NotificationType notificationType)
        {
            var userId = _userManager.GetUserId(User);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.Type == notificationType && !n.IsRead)
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

        [HttpPost("mark-project-notifications/{projectId}")]
        public async Task<IActionResult> MarkProjectNotificationsAsRead(int projectId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId && 
                           n.ProjectId == projectId && 
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

        [HttpPost("mark-all-as-read")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var userId = _userManager.GetUserId(User);

            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (!unreadNotifications.Any()) 
                return Ok(new { success = false, message = "No unread notifications" });

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        private string GetGroupTitle(NotificationType type)
        {
            return type switch
            {
                NotificationType.Message => "Messages",
                NotificationType.VideoInvite => "Video Call Invitations",
                NotificationType.ProjectCreation => "New Projects",
                NotificationType.ProjectMemberAdded => "Project Memberships",
                NotificationType.TaskAssigned => "Task Assignments",
                NotificationType.TaskUpdated => "Task Updates",
                _ => "Other Notifications"
            };
        }
    }
}
