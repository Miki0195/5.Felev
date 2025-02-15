//using Microsoft.AspNetCore.SignalR;
//using System.Threading.Tasks;
//using System.Collections.Generic;
//using Managly.Models;
//using Microsoft.AspNetCore.Identity;
//using Managly.Data;
//using Microsoft.EntityFrameworkCore;

//namespace Managly.Hubs
//{
//    public class VideoCallHub : Hub
//    {
//        private static Dictionary<string, string> connectedUsers = new Dictionary<string, string>();
//        private readonly ApplicationDbContext _context;
//        private readonly UserManager<User> _userManager;

//        public VideoCallHub(ApplicationDbContext context, UserManager<User> userManager)
//        {
//            _context = context;
//            _userManager = userManager;
//        }

//        public override async Task OnConnectedAsync()
//        {
//            string userId = Context.UserIdentifier;
//            connectedUsers[userId] = Context.ConnectionId;
//            await base.OnConnectedAsync();
//        }

//        public override async Task OnDisconnectedAsync(Exception exception)
//        {
//            string userId = Context.UserIdentifier;
//            connectedUsers.Remove(userId);
//            await base.OnDisconnectedAsync(exception);
//        }

//        public async Task SendCallRequest(string receiverId)
//        {
//            var sender = await _userManager.GetUserAsync(Context.User);
//            if (sender == null)
//            {
//                Console.WriteLine("❌ Error: Sender not found.");
//                return;
//            }

//            string senderName = sender.Name + " " + sender.LastName;

//            if (connectedUsers.TryGetValue(receiverId, out string connectionId))
//            {
//                // ✅ Send real-time call invite
//                await Clients.Client(connectionId).SendAsync("ReceiveCallRequest", Context.UserIdentifier, senderName);
//            }

//            try
//            {
//                // ✅ Save notification in the database
//                var newNotification = new Notification
//                {
//                    UserId = receiverId,
//                    Message = $"{senderName} invited you to a video call.",
//                    Link = "/videoconference",
//                    IsRead = false,
//                    Timestamp = DateTime.UtcNow
//                };

//                _context.Notifications.Add(newNotification);
//                int affectedRows = await _context.SaveChangesAsync(); // ✅ Ensure it saves

//                if (affectedRows == 0)
//                {
//                    Console.WriteLine("❌ Error: Notification was not saved to the database.");
//                }
//                else
//                {
//                    Console.WriteLine($"✅ Notification saved successfully. ID: {newNotification.Id}");
//                }

//                // ✅ Fetch unread count for the receiver
//                int unreadCount = await _context.Notifications
//                    .Where(n => n.UserId == receiverId && !n.IsRead)
//                    .CountAsync();

//                // ✅ Send real-time update to the receiver
//                if (connectedUsers.TryGetValue(receiverId, out string notifyConnectionId))
//                {
//                    await Clients.Client(notifyConnectionId).SendAsync("ReceiveNotification",
//                        newNotification.Message, newNotification.Link, unreadCount, 1, newNotification.Timestamp);
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"❌ Database Save Error: {ex.Message}");
//            }
//        }



//        public async Task SendSignal(string receiverId, string signalData)
//        {
//            if (connectedUsers.TryGetValue(receiverId, out string connectionId))
//            {
//                await Clients.Client(connectionId).SendAsync("ReceiveSignal", Context.UserIdentifier, signalData);
//            }
//        }

//        public async Task StartCall(string receiverId)
//        {
//            if (connectedUsers.TryGetValue(receiverId, out string connectionId))
//            {
//                await Clients.Client(connectionId).SendAsync("CallStarted", Context.UserIdentifier);
//            }
//        }
//    }
//}
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
            connectedUsers[userId] = Context.ConnectionId;
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string userId = Context.UserIdentifier;
            connectedUsers.Remove(userId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendCallRequest(string receiverId)
        {
            var sender = await _userManager.GetUserAsync(Context.User);
            if (sender == null)
            {
                Console.WriteLine("❌ Error: Sender not found.");
                return;
            }

            string senderName = sender.Name + " " + sender.LastName;

            // ✅ Generate a Call ID (Always Created, Even If Receiver is Offline)
            var newCall = new VideoConference
            {
                CallerId = sender.Id,
                ReceiverId = receiverId,
                StartTime = DateTime.UtcNow
            };

            _context.VideoConferences.Add(newCall);
            await _context.SaveChangesAsync();

            // ✅ Ensure Sender ALWAYS gets the Call ID
            if (connectedUsers.TryGetValue(sender.Id, out string senderConnectionId))
            {
                await Clients.Client(senderConnectionId).SendAsync("ReceiveCallId", newCall.CallId);
            }

            // ✅ If receiver is online, send the invite
            if (connectedUsers.TryGetValue(receiverId, out string connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveCallRequest", sender.Id, senderName, newCall.CallId);
            }

            try
            {
                // ✅ Save notification in the database
                var newNotification = new Notification
                {
                    UserId = receiverId,
                    Message = $"{senderName} invited you to a video call.",
                    Link = "/videoconference",
                    IsRead = false,
                    Timestamp = DateTime.UtcNow
                };

                _context.Notifications.Add(newNotification);
                int affectedRows = await _context.SaveChangesAsync();

                if (affectedRows == 0)
                {
                    Console.WriteLine("❌ Error: Notification was not saved to the database.");
                }
                else
                {
                    Console.WriteLine($"✅ Notification saved successfully. ID: {newNotification.Id}");
                }

                // ✅ Fetch unread count for the receiver
                int unreadCount = await _context.Notifications
                    .Where(n => n.UserId == receiverId && !n.IsRead)
                    .CountAsync();

                // ✅ Send real-time update to the receiver if they are online
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

        public async Task StartCall(string callId)
        {
            var call = await _context.VideoConferences.FirstOrDefaultAsync(c => c.CallId == callId);
            if (call == null)
            {
                Console.WriteLine("❌ Error: Invalid call attempt.");
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

        public async Task EndCall(string callId)
        {
            var call = await _context.VideoConferences.FirstOrDefaultAsync(c => c.CallId == callId);
            if (call == null)
            {
                Console.WriteLine("❌ Error: Call not found.");
                return;
            }

            call.EndTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var callDuration = call.EndTime.Value - call.StartTime;
            string durationText = $"{callDuration.Minutes} min {callDuration.Seconds} sec";

            // ✅ Notify BOTH users to immediately stop the call
            if (connectedUsers.TryGetValue(call.CallerId, out string callerConnectionId))
            {
                await Clients.Client(callerConnectionId).SendAsync("CallEnded", callId, durationText);
            }

            if (connectedUsers.TryGetValue(call.ReceiverId, out string receiverConnectionId))
            {
                await Clients.Client(receiverConnectionId).SendAsync("CallEnded", callId, durationText);
            }
        }

    }
}
