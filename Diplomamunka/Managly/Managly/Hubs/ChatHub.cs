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
                _userConnections.AddOrUpdate(
                    userId,
                    new HashSet<string> { Context.ConnectionId },
                    (key, oldSet) =>
                    {
                        oldSet.Add(Context.ConnectionId);
                        return oldSet;
                    });

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
                if (_userConnections.TryGetValue(userId, out var connections))
                {
                    connections.Remove(Context.ConnectionId);
                    
                    if (connections.Count == 0)
                    {
                        _userConnections.TryRemove(userId, out _);
                        
                        await Clients.All.SendAsync("UserOnlineStatusChanged", userId, false);
                    }
                    else
                    {
                        Console.WriteLine($"[SignalR] User {userId} still has {connections.Count} active connections");
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string senderId, string receiverId, string message)
        {
            try
            {
                var sender = await _context.Users.FindAsync(senderId);
                var receiver = await _context.Users.FindAsync(receiverId);

                if (sender == null || receiver == null)
                {
                    Console.WriteLine("❌ [SignalR] SendMessage: Sender or receiver user not found");
                    return;
                }

                Console.WriteLine($"✅ [SignalR] SendMessage: From {senderId} to {receiverId}");
                var messagePreview = message.Length > 30 ? message.Substring(0, 27) + "..." : message;

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

                var unreadCounts = await GetUnreadCounts(receiverId, senderId);

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

                await Clients.User(receiverId).SendAsync("ReceiveNotification", notificationPayload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendMessage: {ex.Message}");
            }
        }

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
                if (string.IsNullOrEmpty(userId))
                {
                    return "User ID is null";
                }

                if (string.IsNullOrEmpty(messageId))
                {
                    return "Message ID is null";
                }

                Console.WriteLine($"✅ [SignalR] MessageDeletedForMe: Message ID {messageId} deleted for user {userId}");
                await Clients.User(userId).SendAsync("MessageDeletedForMe", messageId);

                return "Success";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [SignalR] Error in MessageDeletedForMe: {ex.Message}");
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
