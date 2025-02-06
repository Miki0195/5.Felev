using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Managly.Data;
using Managly.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Managly.Hubs;
using Microsoft.AspNetCore.SignalR;
using SendGrid.Helpers.Mail;

namespace Managly.Controllers
{
    [Authorize]
    [Route("chat")]
    [ApiController]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IHubContext<ChatHub> _hubContext; 

        public ChatController(ApplicationDbContext context, UserManager<User> userManager,IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        [Authorize]
        public async Task<IActionResult> Index(string userId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            ViewBag.CurrentUserId = user.Id;
            ViewBag.SelectedUserId = userId;
            return View();
        }


        [HttpGet("search/{query}")]
        public async Task<IActionResult> SearchUsers(string query)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var users = await _context.Users
                .Where(u => u.CompanyId == currentUser.CompanyId &&
                    (u.Name.Contains(query) || u.LastName.Contains(query) ||
                     (u.Name + " " + u.LastName).Contains(query)) && 
                    u.Id != currentUser.Id)
                .Select(u => new { u.Id, FullName = u.Name + " " + u.LastName }) 
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("messages/{receiverId}")]
        public async Task<IActionResult> GetMessages(string receiverId)
        {
            var userId = _userManager.GetUserId(User);

            var messages = await _context.Messages
                .Where(m => (m.SenderId == userId && m.ReceiverId == receiverId) ||
                            (m.SenderId == receiverId && m.ReceiverId == userId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,
                    Content = m.IsDeleted ? "Deleted message" : m.Content,
                    m.SenderId,
                    m.Timestamp,
                    m.IsDeleted,
                    SenderProfilePicture = _context.Users
                .Where(u => u.Id == m.SenderId)
                .Select(u => u.ProfilePicturePath)
                .FirstOrDefault()
                })
        .ToListAsync();

            return Ok(messages);
        }


        [HttpPost("messages")]
        public async Task<IActionResult> SaveMessage([FromBody] JsonElement jsonData, [FromServices] IHubContext<ChatHub> chatHub)
        {
            try
            {
                if (jsonData.ValueKind == JsonValueKind.Undefined || jsonData.ValueKind == JsonValueKind.Null)
                {
                    return BadRequest(new { error = "Request body is missing." });
                }

                string senderId = jsonData.GetProperty("SenderId").GetString();
                string receiverId = jsonData.GetProperty("ReceiverId").GetString();
                string content = jsonData.GetProperty("Content").GetString();

                if (string.IsNullOrWhiteSpace(senderId) || string.IsNullOrWhiteSpace(receiverId))
                {
                    return BadRequest(new { error = "SenderId and ReceiverId are required." });
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return BadRequest(new { error = "Message content cannot be empty." });
                }

                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = content,
                    Timestamp = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                //await chatHub.Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, content);

                //return Ok(new { success = true, message = "Message saved successfully" });
                await chatHub.Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, content, message.Id);

            // ✅ Return the message ID to the frontend
            return Ok(new { success = true, message = "Message saved successfully", messageId = message.Id });
                }
            catch (KeyNotFoundException)
            {
                return BadRequest(new { error = "Invalid JSON structure: Missing required properties." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpGet("recent-chats")]
        public async Task<IActionResult> GetRecentChats()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var chatUserIds = await _context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId) 
                .Distinct()
                .ToListAsync();

            var usersWithLastMessage = await _context.Users
                .Where(u => chatUserIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    FullName = u.Name + " " + u.LastName,
                    ProfilePicturePath = u.ProfilePicturePath ?? "",
                    LastMessage = _context.Messages
                        .Where(m => (m.SenderId == userId && m.ReceiverId == u.Id) ||
                                    (m.SenderId == u.Id && m.ReceiverId == userId))
                        .OrderByDescending(m => m.Timestamp)
                        .Select(m => new
                        {
                            Content = m.Content ?? "",
                            IsFromUser = m.SenderId == userId,
                            IsRead = m.IsRead,
                            Timestamp = m.Timestamp
                        })
                        .FirstOrDefault(),

                    UnreadCount = _context.Messages
                        .Where(m => m.SenderId == u.Id && m.ReceiverId == userId && !m.IsRead) // ✅ Ensure unread messages are properly counted
                        .Count()
                })
                .OrderByDescending(u => u.LastMessage != null ? u.LastMessage.Content : "")
                .ToListAsync();

            usersWithLastMessage = usersWithLastMessage
                .OrderByDescending(u => u.LastMessage?.Timestamp)
                .ToList();

            return Ok(usersWithLastMessage);
        }

        [HttpPost("mark-as-read/{senderId}")]
        public async Task<IActionResult> MarkMessagesAsRead(string senderId)
        {
            var userId = _userManager.GetUserId(User);

            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == senderId && m.ReceiverId == userId && !m.IsRead)
                .ToListAsync();

            if (!unreadMessages.Any())
                return Ok(new { success = false, message = "No unread messages found" });

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }


        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUser(string userId)
        {
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Id, FullName = u.Name + " " + u.LastName, u.ProfilePicturePath }) 
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            return Ok(user);
        }

        [HttpDelete("messages/delete/{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);

            if (message == null)
                return BadRequest(new { error = "Message not found" });

            message.IsDeleted = true; // ✅ Soft delete the message
            await _context.SaveChangesAsync();

            await _hubContext.Clients.User(message.SenderId).SendAsync("MessageDeleted", messageId);
            await _hubContext.Clients.User(message.ReceiverId).SendAsync("MessageDeleted", messageId);

            return Ok(new { success = true, message = "Message deleted for both users." });
        }

    }
}
