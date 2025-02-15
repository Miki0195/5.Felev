using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Managly.Data;
using Managly.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using Managly.Hubs;

namespace Managly.Controllers
{
    [Authorize]
    [Route("api/videoconference")]
    [ApiController]
    public class VideoConferenceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IHubContext<ChatHub> _hubContext;

        public IActionResult Index()
        {
            return View();
        }

        public VideoConferenceController(ApplicationDbContext context, UserManager<User> userManager, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        [HttpGet("search-users")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { error = "Query cannot be empty" });

            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.CompanyId == null)
                return Unauthorized(new { error = "User not authorized or missing company ID." });

            var users = await _context.Users
                .Where(u => u.CompanyId == user.CompanyId &&
                            ((u.Name + " " + u.LastName).Contains(query) ||
                             u.Name.Contains(query) ||
                             u.LastName.Contains(query)))
                .Select(u => new
                {
                    u.Id,
                    FullName = u.Name + " " + u.LastName
                })
                .ToListAsync();

            if (!users.Any())
                return Ok(new { message = "No users found." });

            return Ok(users);
        }

        [HttpPost("invite")]
        public async Task<IActionResult> SendVideoCallInvitation()
        {
            try
            {
                // ✅ Read raw body and log it
                using var reader = new StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();
                Console.WriteLine($"📩 Raw Request Body: {rawBody}");

                // ✅ Deserialize JSON to check structure
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<VideoCallInvitation>(rawBody);
                if (model == null || string.IsNullOrEmpty(model.ReceiverId))
                {
                    return BadRequest(new { error = "ReceiverId is required." });
                }

                var sender = await _userManager.GetUserAsync(User);
                if (sender == null)
                {
                    return Unauthorized(new { error = "User not authenticated." });
                }

                var newInvitation = new VideoCallInvitation
                {
                    SenderId = sender.Id,
                    ReceiverId = model.ReceiverId,
                    Timestamp = DateTime.UtcNow,
                    IsAccepted = false
                };

                _context.VideoCallInvitations.Add(newInvitation);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.User(model.ReceiverId).SendAsync("ReceiveNotification",
                        $"{sender.Name} {sender.LastName} invited you to a video call.", "/videoconference");


                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error.", details = ex.Message });
            }
        }




        [HttpGet("check-invite")]
        public async Task<IActionResult> CheckForInvites()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("❌ Error: User is null or not authenticated.");
                return Unauthorized(new { error = "User not authenticated." });
            }

            // 🔥 Log user ID for debugging
            Console.WriteLine($"🔍 Checking invites for user: {user.Id}");

            // 🔥 Include Sender in the query to avoid NullReferenceException
            var invitation = await _context.VideoCallInvitations
                .Where(i => i.ReceiverId == user.Id && !i.IsAccepted)
                .Include(i => i.Sender) // ✅ Ensure sender data is loaded
                .OrderByDescending(i => i.Timestamp)
                .FirstOrDefaultAsync();

            if (invitation == null)
            {
                Console.WriteLine("✅ No pending invitations found.");
                return Ok(new { hasInvite = false });
            }

            if (invitation.Sender == null)
            {
                Console.WriteLine("❌ Error: Invitation found but Sender is NULL!");
                return StatusCode(500, new { error = "Invalid invitation data: Sender is missing." });
            }

            return Ok(new
            {
                hasInvite = true,
                senderName = $"{invitation.Sender.Name} {invitation.Sender.LastName}",
                senderId = invitation.SenderId
            });
        }



        [HttpPost("accept-invite/{senderId}")]
        public async Task<IActionResult> AcceptInvite(string senderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized(new { error = "User not authenticated." });

            var invitation = await _context.VideoCallInvitations
                .Where(i => i.ReceiverId == user.Id && i.SenderId == senderId && !i.IsAccepted)
                .FirstOrDefaultAsync();

            if (invitation == null) return BadRequest(new { error = "No pending invitation found." });

            invitation.IsAccepted = true;
            await _context.SaveChangesAsync();

            // 🔥 Notify users to start the call
            await _hubContext.Clients.User(senderId).SendAsync("CallStarted", user.Id);
            await _hubContext.Clients.User(user.Id).SendAsync("CallStarted", senderId);

            // ✅ Return JSON instead of redirecting
            return Ok(new { success = true });
        }




    }
}
