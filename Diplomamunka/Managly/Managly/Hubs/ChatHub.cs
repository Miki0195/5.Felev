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

            var sender = await _context.Users.FindAsync(senderId);
            var receiver = await _context.Users.FindAsync(receiverId);

            if (sender == null || receiver == null)
            {
                Console.WriteLine("❌ Sender or Receiver is null!");
                return;
            }

            Console.WriteLine($"📩 Creating notification for {receiver.Name} ({receiverId})");

            var notification = new Notification
            {
                UserId = receiverId,
                Message = $"{sender.Name} sent you a new message",
                Link = $"/chat?userId={senderId}",
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            try
            {
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                int unreadCount = await _context.Notifications
                    .Where(n => n.UserId == receiverId && !n.IsRead)
                    .CountAsync();

                int senderUnreadCount = await _context.Notifications
                    .Where(n => n.UserId == receiverId && !n.IsRead && n.Link.Contains($"userId={senderId}"))
                    .CountAsync();

                Console.WriteLine($"🔔 Sending real-time notification with unread count: {unreadCount}");

                await Clients.User(receiverId).SendAsync("ReceiveNotification", notification.Message, notification.Link, unreadCount, senderUnreadCount, notification.Timestamp);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database Save Error: {ex.Message}");
            }
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
