using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Managly.Models;
using Microsoft.AspNetCore.Identity;
using Managly.Data;
using Microsoft.EntityFrameworkCore;

namespace Managly.Hubs
{
    public class VideoCallHub : Hub
    {
        private static Dictionary<string, string> connectedUsers = new Dictionary<string, string>();
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public VideoCallHub(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public override async Task OnConnectedAsync()
        {
            string userId = Context.UserIdentifier;
            if (!connectedUsers.ContainsKey(userId))
            {
                connectedUsers[userId] = Context.ConnectionId;
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string userId = Context.UserIdentifier;
            if (connectedUsers.ContainsKey(userId))
            {
                connectedUsers.Remove(userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendCallRequest(string receiverId)
        {
            var sender = await _userManager.GetUserAsync(Context.User);
            if (sender == null)
            {
                return;
            }

            string senderName = sender.Name + " " + sender.LastName;

            var existingCall = await _context.VideoConferences
                .Where(c => c.CallerId == sender.Id && c.ReceiverId == receiverId && c.Status == CallStatus.Pending)
                .FirstOrDefaultAsync();

            if (existingCall == null)
            {
                return;
            }

            if (connectedUsers.TryGetValue(sender.Id, out string senderConnectionId))
            {
                await Clients.Client(senderConnectionId).SendAsync("ReceiveCallId", existingCall.CallId);
            }

            if (connectedUsers.TryGetValue(receiverId, out string connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveCallRequest", sender.Id, senderName, existingCall.CallId);
            }

            try
            {
                var newNotification = new Notification
                {
                    UserId = receiverId,
                    Message = $"{senderName} invited you to a video call.",
                    Link = "/videoconference",
                    IsRead = false,
                    Timestamp = DateTime.UtcNow
                };

                _context.Notifications.Add(newNotification);
                await _context.SaveChangesAsync();

                int unreadCount = await _context.Notifications
                    .Where(n => n.UserId == receiverId && !n.IsRead)
                    .CountAsync();

                if (connectedUsers.TryGetValue(receiverId, out string notifyConnectionId))
                {
                    await Clients.Client(notifyConnectionId).SendAsync("ReceiveNotification",
                        newNotification.Message, newNotification.Link, unreadCount, 1, newNotification.Timestamp);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database Save Error: {ex.Message}");
            }
        }


        public async Task SendSignal(string receiverId, string signalData)
        {
            if (connectedUsers.TryGetValue(receiverId, out string connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveSignal", Context.UserIdentifier, signalData);
            }
        }

        public async Task AcceptCall(string callId)
        {
            var call = await _context.VideoConferences.FindAsync(callId);
            if (call == null)
            {
                return;
            }

            if (call.Status == CallStatus.Pending)
            {
                call.Status = CallStatus.Active;
                await _context.SaveChangesAsync();
            }

            if (connectedUsers.TryGetValue(call.CallerId, out string callerConnectionId))
            {
                await Clients.Client(callerConnectionId).SendAsync("CallStarted", call.CallId);
            }

            if (connectedUsers.TryGetValue(call.ReceiverId, out string receiverConnectionId))
            {
                await Clients.Client(receiverConnectionId).SendAsync("CallStarted", call.CallId);
            }
        }

        public async Task EndCall(string callId)
        {
            var call = await _context.VideoConferences.FirstOrDefaultAsync(c => c.CallId == callId);
            if (call == null)
            {
                return;
            }

            call.IsEnded = true;
            call.EndTime = DateTime.UtcNow;
            call.Status = CallStatus.Ended;
            await _context.SaveChangesAsync();

            var callDuration = call.EndTime.Value - call.StartTime;
            string durationText = $"{callDuration.Minutes} min {callDuration.Seconds} sec";

            if (connectedUsers.TryGetValue(call.CallerId, out string callerConnectionId))
            {
                await Clients.Client(callerConnectionId).SendAsync("CallEnded", callId, durationText);
            }

            if (connectedUsers.TryGetValue(call.ReceiverId, out string receiverConnectionId))
            {
                await Clients.Client(receiverConnectionId).SendAsync("CallEnded", callId, durationText);
            }
        }

        public async Task CheckCallStatus(string callId)
        {
            var call = await _context.VideoConferences.FindAsync(callId);
            if (call == null)
            {
                return;
            }

            string statusMessage = call.Status switch
            {
                CallStatus.Pending => "Call is pending",
                CallStatus.Active => "Call is active",
                CallStatus.Missed => "Call was missed",
                CallStatus.Ended => "Call has ended",
                _ => "Unknown status"
            };

            if (connectedUsers.TryGetValue(call.CallerId, out string callerConnectionId))
            {
                await Clients.Client(callerConnectionId).SendAsync("ReceiveCallStatus", callId, statusMessage);
            }

            if (connectedUsers.TryGetValue(call.ReceiverId, out string receiverConnectionId))
            {
                await Clients.Client(receiverConnectionId).SendAsync("ReceiveCallStatus", callId, statusMessage);
            }
        }

        public async Task StartCall(string callId)
        {
            var call = await _context.VideoConferences.FirstOrDefaultAsync(c => c.CallId == callId);
            if (call == null)
            {
                return;
            }

            if (connectedUsers.TryGetValue(call.ReceiverId, out string receiverConnectionId))
            {
                await Clients.Client(receiverConnectionId).SendAsync("CallStarted", call.CallId);
            }

            if (connectedUsers.TryGetValue(call.CallerId, out string callerConnectionId))
            {
                await Clients.Client(callerConnectionId).SendAsync("CallStarted", call.CallId);
            }
            }
    }
}
