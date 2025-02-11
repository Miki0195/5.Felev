using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Managly.Hubs
{
    public class VideoCallHub : Hub
    {
        private static Dictionary<string, string> connectedUsers = new Dictionary<string, string>();

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
            if (connectedUsers.TryGetValue(receiverId, out string connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveCallRequest", Context.UserIdentifier);
            }
        }

        public async Task SendSignal(string receiverId, string signalData)
        {
            if (connectedUsers.TryGetValue(receiverId, out string connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveSignal", Context.UserIdentifier, signalData);
            }
        }

        public async Task StartCall(string receiverId)
        {
            if (connectedUsers.TryGetValue(receiverId, out string connectionId))
            {
                await Clients.Client(connectionId).SendAsync("CallStarted", Context.UserIdentifier);
            }
        }
    }
}
