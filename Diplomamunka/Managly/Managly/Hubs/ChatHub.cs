using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Managly.Models;
using Managly.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;
using Managly.Models.Enums;
using Org.BouncyCastle.Cms;
using System.Text.Json;

namespace Managly.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = 
            new ConcurrentDictionary<string, HashSet<string>>();

        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            string userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                // Add connection to user's connection set
                _userConnections.AddOrUpdate(
                    userId,
                    new HashSet<string> { Context.ConnectionId },
                    (key, oldSet) =>
                    {
                        oldSet.Add(Context.ConnectionId);
                        return oldSet;
                    });

                Console.WriteLine($"✅ [SignalR] User {userId} connected with connection ID: {Context.ConnectionId}");
                Console.WriteLine($"✅ [SignalR] Active connections for user {userId}: {_userConnections[userId].Count}");

                // Broadcast user's online status to all clients
                await Clients.All.SendAsync("UserOnlineStatusChanged", userId, true);
            }
            else
            {
                Console.WriteLine("❌ [SignalR] Error: UserIdentifier is null in OnConnectedAsync");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                // Remove this connection from user's connection set
                if (_userConnections.TryGetValue(userId, out var connections))
                {
                    connections.Remove(Context.ConnectionId);
                    
                    // If user has no more connections, remove them from dictionary
                    if (connections.Count == 0)
                    {
                        _userConnections.TryRemove(userId, out _);
                        
                        // Only broadcast offline status if user has no more active connections
                        Console.WriteLine($"✅ [SignalR] User {userId} is now offline (no active connections)");
                        await Clients.All.SendAsync("UserOnlineStatusChanged", userId, false);
                    }
                    else
                    {
                        Console.WriteLine($"✅ [SignalR] User {userId} still has {connections.Count} active connections");
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string senderId, string receiverId, string message)
        {
            Console.WriteLine("✅ SendMessage method called");

            try
            {
                var sender = await _context.Users.FindAsync(senderId);
                var receiver = await _context.Users.FindAsync(receiverId);

                if (sender == null || receiver == null)
                {
                    Console.WriteLine("❌ Sender or Receiver is null!");
                    return;
                }

                Console.WriteLine($"📩 Creating notification for {receiver.Name} ({receiverId})");

                // Create message preview (assuming you have this logic)
                var messagePreview = message.Length > 30 ? message.Substring(0, 27) + "..." : message;

                // Create the notification with the new structure
                var notification = new Notification
                {
                    UserId = receiverId,
                    Message = $"New message from {sender.Name}",
                    Link = $"/chat?userId={senderId}",
                    Type = NotificationType.Message,
                    RelatedUserId = senderId,
                    Timestamp = DateTime.Now,
                    MetaData = JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        { "senderName", sender.Name },
                        { "messagePreview", messagePreview },
                        { "senderId", senderId },
                        { "senderProfilePicture", sender.ProfilePicturePath ?? "" }
                    })
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Get notification counts for the specific type
                var unreadCounts = await GetUnreadCounts(receiverId, senderId);

                // Create notification payload for SignalR
                var notificationPayload = new
                {
                    id = notification.Id,
                    message = notification.Message,
                    link = notification.Link,
                    type = notification.Type,
                    timestamp = notification.Timestamp,
                    metaData = JsonSerializer.Deserialize<Dictionary<string, string>>(notification.MetaData),
                    unreadCount = unreadCounts.totalUnread,
                    typeUnreadCount = unreadCounts.typeUnread,
                    senderUnreadCount = unreadCounts.senderUnread
                };

                Console.WriteLine($"🔔 Sending real-time notification with unread count: {unreadCounts.totalUnread}");

                // Send the notification through SignalR
                await Clients.User(receiverId).SendAsync("ReceiveNotification", notificationPayload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in SendMessage: {ex.Message}");
                // Consider throwing the exception or handling it according to your error handling strategy
            }
        }

        // Helper method to get various unread counts
        private async Task<(int totalUnread, int typeUnread, int senderUnread)> GetUnreadCounts(string userId, string senderId)
        {
            var totalUnread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();

            var typeUnread = await _context.Notifications
                .Where(n => n.UserId == userId &&
                           !n.IsRead &&
                           n.Type == NotificationType.Message)
                .CountAsync();

            var senderUnread = await _context.Notifications
                .Where(n => n.UserId == userId &&
                           !n.IsRead &&
                           n.Type == NotificationType.Message &&
                           n.RelatedUserId == senderId)
                .CountAsync();

            return (totalUnread, typeUnread, senderUnread);
        }

        public async Task MessageDeleted(string messageId)
        {
            Console.WriteLine($"🔴 Deleting message {messageId} for all clients...");

            await Clients.All.SendAsync("MessageDeleted", messageId);
        }

        public async Task DeleteMessage(string messageId, string senderId, string receiverId)
        {
            var message = await _context.Messages.FindAsync(Guid.Parse(messageId));

            if (message != null)
            {
                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();

                await Clients.User(senderId).SendAsync("MessageDeleted", messageId);
                await Clients.User(receiverId).SendAsync("MessageDeleted", messageId);
            }
        }

        public async Task<string> MessageDeletedForMe(string messageId, string userId)
        {
            try
            {
                Console.WriteLine($"🔴 [SignalR] Trying to delete message {messageId} for user {userId}");

                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("❌ [SignalR] Error: User ID is null in MessageDeletedForMe");
                    return "User ID is null";
                }

                if (string.IsNullOrEmpty(messageId))
                {
                    Console.WriteLine("❌ [SignalR] Error: Message ID is null in MessageDeletedForMe");
                    return "Message ID is null";
                }

                Console.WriteLine($"✅ [SignalR] Sending MessageDeletedForMe event to user {userId}");
                await Clients.User(userId).SendAsync("MessageDeletedForMe", messageId);

                return "Success";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [SignalR] Exception in MessageDeletedForMe: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        public bool IsUserOnline(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            
            return _userConnections.TryGetValue(userId, out var connections) && connections.Count > 0;
        }
    }
}
