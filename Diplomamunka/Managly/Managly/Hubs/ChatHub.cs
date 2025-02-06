using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Managly.Models;
using Managly.Data;
using Microsoft.EntityFrameworkCore;

namespace Managly.Hubs
{
    public class ChatHub : Hub
    {
        private static Dictionary<string, string> _connections = new Dictionary<string, string>();

        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context; 
        }
        public override Task OnConnectedAsync()
        {
            Console.WriteLine("✅ ChatHub Connected!");
            string userId = Context.UserIdentifier;
            if (userId != null && !_connections.ContainsKey(userId))
            {
                _connections[userId] = Context.ConnectionId;
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            string userId = Context.UserIdentifier;
            if (userId != null && _connections.ContainsKey(userId))
            {
                _connections.Remove(userId);
            }
            return base.OnDisconnectedAsync(exception);
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

        public async Task MessageDeleted(string messageId)
        {
            Console.WriteLine($"🔴 Deleting message {messageId} for all clients...");

            await Clients.All.SendAsync("MessageDeleted", messageId);
        }

        public async Task MessageDeletedForMe(string messageId, string userId)
        {
            Console.WriteLine($"🔴 Message {messageId} deleted for user {userId}");

            await Clients.User(userId).SendAsync("MessageDeletedForMe", messageId);
        }



    }
}
