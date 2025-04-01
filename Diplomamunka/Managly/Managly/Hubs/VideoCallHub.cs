using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Managly.Models;
using Microsoft.AspNetCore.Identity;
using Managly.Data;
using Microsoft.EntityFrameworkCore;
using Managly.Models.Enums;
using System.Text.Json;

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
            if (Context.UserIdentifier == null)
            {
                throw new InvalidOperationException("UserIdentifier cannot be null.");
            }

            string userId = Context.UserIdentifier;
            if (!connectedUsers.ContainsKey(userId))
            {
                connectedUsers[userId] = Context.ConnectionId;
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (Context.UserIdentifier == null)
            {
                throw new InvalidOperationException("UserIdentifier cannot be null.");
            }

            string userId = Context.UserIdentifier;
            if (connectedUsers.ContainsKey(userId))
            {
                connectedUsers.Remove(userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendCallRequest(string receiverId)
        {
            if (Context.User == null)
            {
                throw new InvalidOperationException("User cannot be null.");
            }

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

            if (connectedUsers.TryGetValue(sender.Id, out string? senderConnectionId) && senderConnectionId is not null)
            {
                await Clients.Client(senderConnectionId).SendAsync("ReceiveCallId", existingCall.CallId);
            }

            if (connectedUsers.TryGetValue(receiverId, out string? connectionId) && connectionId is not null)
            {
                await Clients.Client(connectionId).SendAsync("ReceiveCallRequest", sender.Id, senderName, existingCall.CallId);
            }
        }


        public async Task SendSignal(string receiverId, string signalData)
        {
            if (connectedUsers.TryGetValue(receiverId, out string? connectionId) && connectionId is not null)
            {
                await Clients.Client(connectionId).SendAsync("ReceiveSignal", Context.UserIdentifier, signalData);
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

            if (connectedUsers.TryGetValue(call.CallerId, out string? callerConnectionId) && callerConnectionId is not null)
            {
                await Clients.Client(callerConnectionId).SendAsync("CallEnded", callId, durationText);
            }

            if (connectedUsers.TryGetValue(call.ReceiverId, out string? receiverConnectionId) && receiverConnectionId is not null)
            {
                await Clients.Client(receiverConnectionId).SendAsync("CallEnded", callId, durationText);
            }
        }

        public async Task StartCall(string callId)
        {
            var call = await _context.VideoConferences.FirstOrDefaultAsync(c => c.CallId == callId);
            if (call == null)
            {
                return;
            }

            if (connectedUsers.TryGetValue(call.ReceiverId, out string? receiverConnectionId) && receiverConnectionId is not null)
            {
                await Clients.Client(receiverConnectionId).SendAsync("CallStarted", call.CallId);
            }

            if (connectedUsers.TryGetValue(call.CallerId, out string? callerConnectionId) && callerConnectionId is not null)
            {
                await Clients.Client(callerConnectionId).SendAsync("CallStarted", call.CallId);
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

            if (connectedUsers.TryGetValue(call.CallerId, out string? callerConnectionId) && callerConnectionId is not null)
            {
                await Clients.Client(callerConnectionId).SendAsync("ReceiveCallStatus", callId, statusMessage);
            }

            if (connectedUsers.TryGetValue(call.ReceiverId, out string? receiverConnectionId) && receiverConnectionId is not null)
            {
                await Clients.Client(receiverConnectionId).SendAsync("ReceiveCallStatus", callId, statusMessage);
            }
        }
    }
}


