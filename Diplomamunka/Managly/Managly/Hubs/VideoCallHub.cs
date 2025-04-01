using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Managly.Models;
using Microsoft.AspNetCore.Identity;
using Managly.Data;
using Microsoft.EntityFrameworkCore;
using Managly.Models.Enums;

namespace Managly.Hubs
{
    public class VideoCallHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> ConnectedUsers = new();
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public VideoCallHub(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public override async Task OnConnectedAsync()
        {
            if (string.IsNullOrEmpty(Context.UserIdentifier))
            {
                throw new InvalidOperationException("User not authenticated");
            }

            ConnectedUsers.AddOrUpdate(Context.UserIdentifier, Context.ConnectionId, (_, __) => Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (!string.IsNullOrEmpty(Context.UserIdentifier))
            {
                ConnectedUsers.TryRemove(Context.UserIdentifier, out _);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task<bool> SendCallRequest(string receiverId)
        {
            try
            {
                if (string.IsNullOrEmpty(receiverId))
                {
                    throw new ArgumentException("ReceiverId cannot be null or empty", nameof(receiverId));
                }

                if (Context.User == null)
                {
                    throw new InvalidOperationException("User not authenticated");
                }

                var sender = await _userManager.GetUserAsync(Context.User);
                if (sender == null)
                {
                    return false;
                }

                string senderName = $"{sender.Name} {sender.LastName}";

                var existingCall = await _context.VideoConferences
                    .Where(c => c.CallerId == sender.Id && c.ReceiverId == receiverId && c.Status == CallStatus.Pending)
                    .FirstOrDefaultAsync();

                if (existingCall == null)
                {
                    return false;
                }

                if (ConnectedUsers.TryGetValue(sender.Id, out string? senderConnectionId))
                {
                    await Clients.Client(senderConnectionId).SendAsync("ReceiveCallId", existingCall.CallId);
                }

                if (ConnectedUsers.TryGetValue(receiverId, out string? receiverConnectionId))
                {
                    await Clients.Client(receiverConnectionId).SendAsync("ReceiveCallRequest", sender.Id, senderName, existingCall.CallId);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendCallRequest: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendSignal(string receiverId, string signalData)
        {
            try
            {
                if (string.IsNullOrEmpty(receiverId) || string.IsNullOrEmpty(signalData))
                {
                    throw new ArgumentException("ReceiverId and signalData cannot be null or empty");
                }

                if (ConnectedUsers.TryGetValue(receiverId, out string? connectionId))
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveSignal", Context.UserIdentifier, signalData);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendSignal: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EndCall(string callId)
        {
            try
            {
                if (string.IsNullOrEmpty(callId))
                {
                    throw new ArgumentException("CallId cannot be null or empty", nameof(callId));
                }

                var call = await _context.VideoConferences.FirstOrDefaultAsync(c => c.CallId == callId);
                if (call == null)
                {
                    return false;
                }

                call.IsEnded = true;
                call.EndTime = DateTime.Now;
                call.Status = CallStatus.Ended;
                await _context.SaveChangesAsync();

                var callDuration = call.EndTime.Value - call.StartTime;
                string durationText = $"{callDuration.Minutes} min {callDuration.Seconds} sec";

                if (ConnectedUsers.TryGetValue(call.CallerId, out string? callerConnectionId))
                {
                    await Clients.Client(callerConnectionId).SendAsync("CallEnded", callId, durationText);
                }

                if (ConnectedUsers.TryGetValue(call.ReceiverId, out string? receiverConnectionId))
                {
                    await Clients.Client(receiverConnectionId).SendAsync("CallEnded", callId, durationText);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EndCall: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> StartCall(string callId)
        {
            try
            {
                if (string.IsNullOrEmpty(callId))
                {
                    throw new ArgumentException("CallId cannot be null or empty", nameof(callId));
                }

                var call = await _context.VideoConferences.FirstOrDefaultAsync(c => c.CallId == callId);
                if (call == null)
                {
                    return false;
                }

                if (ConnectedUsers.TryGetValue(call.ReceiverId, out string? receiverConnectionId))
                {
                    await Clients.Client(receiverConnectionId).SendAsync("CallStarted", call.CallId);
                }

                if (ConnectedUsers.TryGetValue(call.CallerId, out string? callerConnectionId))
                {
                    await Clients.Client(callerConnectionId).SendAsync("CallStarted", call.CallId);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in StartCall: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CheckCallStatus(string callId)
        {
            try
            {
                if (string.IsNullOrEmpty(callId))
                {
                    throw new ArgumentException("CallId cannot be null or empty", nameof(callId));
                }

                var call = await _context.VideoConferences.FindAsync(callId);
                if (call == null)
                {
                    return false;
                }

                string statusMessage = call.Status switch
                {
                    CallStatus.Pending => "Call is pending",
                    CallStatus.Active => "Call is active",
                    CallStatus.Missed => "Call was missed",
                    CallStatus.Ended => "Call has ended",
                    _ => "Unknown status"
                };

                if (ConnectedUsers.TryGetValue(call.CallerId, out string? callerConnectionId))
                {
                    await Clients.Client(callerConnectionId).SendAsync("ReceiveCallStatus", callId, statusMessage);
                }

                if (ConnectedUsers.TryGetValue(call.ReceiverId, out string? receiverConnectionId))
                {
                    await Clients.Client(receiverConnectionId).SendAsync("ReceiveCallStatus", callId, statusMessage);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CheckCallStatus: {ex.Message}");
                return false;
            }
        }
    }
}


