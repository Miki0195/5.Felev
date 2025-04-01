using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Managly.Data;
using Managly.Models;
using Managly.Models.VideoConference;
using Managly.Models.Enums;
using Microsoft.AspNetCore.SignalR;
using System;
using System.IO;
using Managly.Hubs;
using System.Collections.Generic;
using System.Text.Json;

namespace Managly.Controllers
{
    [Authorize]
    [Route("api/videoconference")]
    [ApiController]
    public class VideoConferenceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IHubContext<VideoCallHub> _hubContext;

        public VideoConferenceController(ApplicationDbContext context, UserManager<User> userManager, IHubContext<VideoCallHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            return View();
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
                using var reader = new StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();

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
                    Timestamp = DateTime.Now,
                    IsAccepted = false
                };

                _context.VideoCallInvitations.Add(newInvitation);
                await _context.SaveChangesAsync();

                var newCall = new VideoConference
                {
                    CallId = Guid.NewGuid().ToString(),
                    CallerId = sender.Id,
                    ReceiverId = model.ReceiverId,
                    StartTime = DateTime.Now,
                    Status = CallStatus.Pending,
                    IsEnded = false
                };

                _context.VideoConferences.Add(newCall);
                await _context.SaveChangesAsync();

                var notification = new Notification
                {
                    UserId = model.ReceiverId,
                    Message = $"{sender.Name} {sender.LastName} is calling you!",
                    Link = "api/videoconference",
                    Type = NotificationType.VideoInvite,
                    IsRead = false,
                    Timestamp = DateTime.Now,
                    RelatedUserId = sender.Id,
                    MetaData = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        { "senderName", $"{sender.Name} {sender.LastName}" }
                    })
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                var unreadCount = await _context.Notifications
                    .Where(n => n.UserId == model.ReceiverId && !n.IsRead)
                    .CountAsync();

                await _hubContext.Clients.User(model.ReceiverId).SendAsync("ReceiveNotification",
                    new
                    {
                        message = notification.Message,
                        link = notification.Link,
                        type = notification.Type,
                        relatedUserId = notification.RelatedUserId,
                        metaData = new Dictionary<string, string>
                        {
                            { "senderName", $"{sender.Name} {sender.LastName}" }
                        },
                        timestamp = notification.Timestamp

                    });

                return Ok(new { success = true, callId = newCall.CallId });
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
                return Unauthorized(new { error = "User not authenticated." });
            }

            var invitation = await _context.VideoCallInvitations
                .Where(i => i.ReceiverId == user.Id && !i.IsAccepted)
                .Include(i => i.Sender)
                .OrderByDescending(i => i.Timestamp)
                .FirstOrDefaultAsync();

            if (invitation == null)
            {
                return Ok(new { hasInvite = false });
            }

            if (invitation.Sender == null)
            {
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

            var existingCall = await _context.VideoConferences
                .Where(c => c.CallerId == senderId && c.ReceiverId == user.Id && c.Status == CallStatus.Pending)
                .FirstOrDefaultAsync();

            if (existingCall == null)
            {
                return BadRequest(new { error = "Call session not found." });
            }

            existingCall.Status = CallStatus.Active;
            await _context.SaveChangesAsync();

            await _hubContext.Clients.User(senderId).SendAsync("CallStarted", existingCall.CallId);
            await _hubContext.Clients.User(user.Id).SendAsync("CallStarted", existingCall.CallId);

            return Ok(new { success = true, callId = existingCall.CallId });
        }

        [HttpPost("end-call/{callId}")]
        public async Task<IActionResult> EndCall(string callId)
        {
            var call = await _context.VideoConferences.FindAsync(callId);
            if (call == null)
            {
                return NotFound(new { error = "Call not found." });
            }

            if (!call.IsEnded)
            {
                call.IsEnded = true;
                call.EndTime = DateTime.Now;

                var pendingInvitation = await _context.VideoCallInvitations
                    .Where(i => i.ReceiverId == call.ReceiverId && i.SenderId == call.CallerId && !i.IsAccepted)
                    .FirstOrDefaultAsync();

                if (pendingInvitation != null)
                {
                    pendingInvitation.IsAccepted = true;
                }

                var callingNotifications = await _context.Notifications
                    .Where(n => n.UserId == call.ReceiverId &&
                           n.RelatedUserId == call.CallerId &&
                           n.Type == NotificationType.VideoInvite &&
                           n.Message.Contains("calling"))
                    .ToListAsync();

                if (callingNotifications.Any())
                {
                    foreach (var notification in callingNotifications)
                    {
                        _context.Notifications.Remove(notification);
                    }
                    await _context.SaveChangesAsync();
                }

                if (call.Status == CallStatus.Pending)
                {
                    call.Status = CallStatus.Missed;

                    var caller = await _context.Users.FindAsync(call.CallerId);
                    var receiver = await _context.Users.FindAsync(call.ReceiverId);

                    if (caller != null && receiver != null)
                    {
                        var missedCallNotification = new Notification
                        {
                            UserId = receiver.Id,
                            Message = $"Missed call from {caller.Name} {caller.LastName}.",
                            Link = "api/videoconference",
                            Type = NotificationType.VideoInvite,
                            IsRead = false,
                            Timestamp = DateTime.Now,
                            RelatedUserId = caller.Id,
                            MetaData = JsonSerializer.Serialize(new Dictionary<string, string>
                            {
                                { "senderName", $"{caller.Name} {caller.LastName}" }
                            })
                        };
                        _context.Notifications.Add(missedCallNotification);
                        await _context.SaveChangesAsync();

                        await _hubContext.Clients.User(receiver.Id).SendAsync("ReceiveNotification",
                            new
                            {
                                message = missedCallNotification.Message,
                                link = missedCallNotification.Link,
                                type = missedCallNotification.Type,
                                relatedUserId = missedCallNotification.RelatedUserId,
                                metaData = new Dictionary<string, string>
                                {
                                    { "senderName", $"{caller.Name} {caller.LastName}" }
                                },
                                timestamp = missedCallNotification.Timestamp

                            });
                    }
                }
                else
                {
                    call.Status = CallStatus.Ended;
                }

                await _context.SaveChangesAsync();
            }

            string message = call.Status == CallStatus.Missed ? "Missed Call" : "Call Ended";

            if (call.EndTime.HasValue && call.Status == CallStatus.Ended)
            {
                var callDuration = call.EndTime.Value - call.StartTime;
                message = $"{callDuration.Minutes} min {callDuration.Seconds} sec";
            }

            await _hubContext.Clients.User(call.CallerId).SendAsync("CallEnded", callId, message);
            await _hubContext.Clients.User(call.ReceiverId).SendAsync("CallEnded", callId, message);

            return Ok(new { success = true, message });
        }

        [HttpGet("get-call/{callId}")]
        public async Task<IActionResult> GetCall(string callId)
        {
            var call = await _context.VideoConferences.FindAsync(callId);
            if (call == null) return NotFound(new { error = "Call not found." });

            return Ok(call);
        }

        [HttpGet("get-user-calls")]
        public async Task<IActionResult> GetUserCalls()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized(new { error = "User not authenticated." });

            var calls = await _context.VideoConferences
                .Where(c => c.CallerId == user.Id || c.ReceiverId == user.Id)
                .OrderByDescending(c => c.StartTime)
                .ToListAsync();

            return Ok(calls);
        }

        [HttpGet("get-active-call")]
        public async Task<IActionResult> GetActiveCall()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized(new { error = "User not authenticated." });

            var activeCall = await _context.VideoConferences
                .Where(c => (c.CallerId == user.Id || c.ReceiverId == user.Id) && c.Status == CallStatus.Active && !c.IsEnded)
                .OrderByDescending(c => c.StartTime)
                .FirstOrDefaultAsync();

            if (activeCall == null)
                return Ok(new { hasActiveCall = false });

            return Ok(new
            {
                hasActiveCall = true,
                callId = activeCall.CallId,
                targetUserId = activeCall.CallerId == user.Id ? activeCall.ReceiverId : activeCall.CallerId
            });
        }


        [HttpPost("missed-call/{callId}")]
        public async Task<IActionResult> MissedCall(string callId)
        {
            var call = await _context.VideoConferences.FindAsync(callId);
            if (call == null) return NotFound();

            if (call.Status == CallStatus.Active)
            {
                return Ok(new { success = false, message = "User joined before timeout." });
            }

            if (call.Status == CallStatus.Pending)
            {
                call.Status = CallStatus.Missed;
                call.IsEnded = true;
                call.EndTime = DateTime.Now;
                await _context.SaveChangesAsync();

                var callingNotifications = await _context.Notifications
                    .Where(n => n.UserId == call.ReceiverId &&
                           n.RelatedUserId == call.CallerId &&
                           n.Type == NotificationType.VideoInvite &&
                           n.Message.Contains("calling"))
                    .ToListAsync();

                if (callingNotifications.Any())
                {
                    foreach (var notification in callingNotifications)
                    {
                        _context.Notifications.Remove(notification);
                    }
                    await _context.SaveChangesAsync();
                }

                var caller = await _context.Users.FindAsync(call.CallerId);
                var receiver = await _context.Users.FindAsync(call.ReceiverId);

                if (receiver != null)
                {
                    var missedCallNotification = new Notification
                    {
                        UserId = receiver.Id,
                        Message = $"Missed call from {caller.Name} {caller.LastName}",
                        Link = "api/videoconference",
                        Type = NotificationType.VideoInvite,
                        IsRead = false,
                        Timestamp = DateTime.Now,
                        RelatedUserId = caller.Id,
                        MetaData = JsonSerializer.Serialize(new Dictionary<string, string>
                        {
                            { "senderName", $"{caller.Name} {caller.LastName}" }
                        })
                    };

                    _context.Notifications.Add(missedCallNotification);
                    await _context.SaveChangesAsync();

                    await _hubContext.Clients.User(receiver.Id).SendAsync("CallMissed", caller.Id);

                    await _hubContext.Clients.User(receiver.Id).SendAsync("ReceiveNotification",
                        new
                        {
                            id = missedCallNotification.Id,
                            message = missedCallNotification.Message,
                            link = missedCallNotification.Link,
                            type = missedCallNotification.Type,
                            relatedUserId = missedCallNotification.RelatedUserId,
                            metaData = new Dictionary<string, string>
                            {
                                { "senderName", $"{caller.Name} {caller.LastName}" }
                            },
                            timestamp = missedCallNotification.Timestamp
                        });
                }

                await _hubContext.Clients.User(call.CallerId).SendAsync("CallEnded", callId, "Missed Call");
            }

            return Ok(new { success = true });
        }

        [HttpPost("mark-call-active/{callId}")]
        public async Task<IActionResult> MarkCallActive(string callId)
        {
            var call = await _context.VideoConferences.FindAsync(callId);
            if (call == null)
            {
                return NotFound(new { error = "Call not found." });
            }

            if (!call.IsEnded)
            {
                call.Status = CallStatus.Active;
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }

        [HttpGet("get-recent-calls")]
        public async Task<IActionResult> GetRecentCalls()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized(new { error = "User not authenticated." });

            var recentCalls = await _context.VideoConferences
                .Where(c => c.CallerId == user.Id || c.ReceiverId == user.Id)
                .OrderByDescending(c => c.StartTime)
                .Take(5)
                .Select(c => new
                {
                    CallId = c.CallId,
                    OtherUser = c.CallerId == user.Id
                        ? _context.Users.Where(u => u.Id == c.ReceiverId).Select(u => u.Name + " " + u.LastName).FirstOrDefault()
                        : _context.Users.Where(u => u.Id == c.CallerId).Select(u => u.Name + " " + u.LastName).FirstOrDefault(),
                    Duration = c.IsEnded && c.Duration.HasValue
                        ? $"{c.Duration.Value.Minutes} min {c.Duration.Value.Seconds} sec"
                        : (c.Status == CallStatus.Missed ? "Missed Call" : "Ongoing"),
                    Timestamp = c.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = c.Status.ToString()
                })
                .ToListAsync();

            return Ok(recentCalls);
        }

        [HttpGet("get-call-by-users")]
        public async Task<IActionResult> GetCallByUsers([FromQuery] string senderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized(new { error = "User not authenticated." });

            var call = await _context.VideoConferences
                .Where(c =>
                    (c.CallerId == senderId && c.ReceiverId == user.Id) ||
                    (c.CallerId == user.Id && c.ReceiverId == senderId))
                .OrderByDescending(c => c.StartTime)
                .FirstOrDefaultAsync();

            if (call == null)
            {
                return Ok(new { hasCall = false });
            }

            return Ok(new
            {
                hasCall = true,
                callId = call.CallId,
                status = call.Status.ToString(),
                isEnded = call.IsEnded
            });
        }
    }
}