using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Managly.Data;
using Managly.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Managly.Hubs;
using Microsoft.AspNetCore.SignalR;
using SendGrid.Helpers.Mail;
using System.Security.Claims;

namespace Managly.Controllers
{
    [Authorize]
    [Route("chat")]
    [ApiController]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ChatHub _chatHub;

        public ChatController(
            ApplicationDbContext context, 
            UserManager<User> userManager,
            IHubContext<ChatHub> hubContext,
            ChatHub chatHub)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _chatHub = chatHub;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index([FromQuery]string userId = null, [FromQuery]int? groupId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            ViewBag.CurrentUserId = user.Id;
            ViewBag.SelectedUserId = userId;
            ViewBag.SelectedGroupId = groupId;

            // If it's a group chat, get the group details
            if (groupId.HasValue)
            {
                var group = await _context.GroupChats
                    .Include(g => g.Members)
                    .Where(g => g.Id == groupId && g.Members.Any(m => m.UserId == user.Id))
                    .Select(g => new
                    {
                        g.Id,
                        g.Name,
                        g.CreatedById,
                        CurrentUserRole = new
                        {
                            IsAdmin = g.Members.Any(m => m.UserId == user.Id && m.IsAdmin),
                            IsCreator = g.CreatedById == user.Id
                        }
                    })
                    .FirstOrDefaultAsync();

                if (group != null)
                {
                    ViewBag.GroupInfo = group;
                }
            }

            return View();
        }


        [HttpGet("search/{query}")]
        public async Task<IActionResult> SearchUsers(string query)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var users = await _context.Users
                .Where(u => u.CompanyId == currentUser.CompanyId &&
                    (u.Name.Contains(query) || u.LastName.Contains(query) ||
                     (u.Name + " " + u.LastName).Contains(query)) && 
                    u.Id != currentUser.Id)
                .Select(u => new { u.Id, FullName = u.Name + " " + u.LastName }) 
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("messages/{receiverId}")]
        public async Task<IActionResult> GetMessages(string receiverId)
        {
            var userId = _userManager.GetUserId(User);

            var messages = await _context.Messages
                .Where(m => (m.SenderId == userId && m.ReceiverId == receiverId) ||
                            (m.SenderId == receiverId && m.ReceiverId == userId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    id = m.Id,
                    content = m.IsDeleted ? "Deleted message" : m.Content,
                    senderId = m.SenderId,
                    timestamp = m.Timestamp,
                    isDeleted = m.IsDeleted,
                    senderProfilePicture = _context.Users
                .Where(u => u.Id == m.SenderId)
                .Select(u => u.ProfilePicturePath)
                .FirstOrDefault()
                })
        .ToListAsync();

            return Ok(messages);
        }


        [HttpPost("messages")]
        public async Task<IActionResult> SaveMessage([FromBody] JsonElement jsonData, [FromServices] IHubContext<ChatHub> chatHub)
        {
            try
            {
                if (jsonData.ValueKind == JsonValueKind.Undefined || jsonData.ValueKind == JsonValueKind.Null)
                {
                    return BadRequest(new { success = false, error = "Request body is missing." });
                }

                // Try to get properties with both camelCase and PascalCase
                string senderId = null;
                string receiverId = null;
                string content = null;

                // Check for camelCase properties first (modern JSON convention)
                if (jsonData.TryGetProperty("senderId", out var senderIdElement))
                {
                    senderId = senderIdElement.GetString();
                }
                // Fallback to PascalCase
                else if (jsonData.TryGetProperty("SenderId", out senderIdElement))
                {
                    senderId = senderIdElement.GetString();
                }

                if (jsonData.TryGetProperty("receiverId", out var receiverIdElement))
                {
                    receiverId = receiverIdElement.GetString();
                }
                else if (jsonData.TryGetProperty("ReceiverId", out receiverIdElement))
                {
                    receiverId = receiverIdElement.GetString();
                }

                if (jsonData.TryGetProperty("content", out var contentElement))
                {
                    content = contentElement.GetString();
                }
                else if (jsonData.TryGetProperty("Content", out contentElement))
                {
                    content = contentElement.GetString();
                }

                if (string.IsNullOrWhiteSpace(senderId) || string.IsNullOrWhiteSpace(receiverId))
                {
                    return BadRequest(new { success = false, error = "SenderId and ReceiverId are required." });
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return BadRequest(new { success = false, error = "Message content cannot be empty." });
                }

                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = content,
                    Timestamp = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                await chatHub.Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, content, message.Id);

                // Return the message ID in a consistent format
                return Ok(new { 
                    success = true, 
                    message = "Message saved successfully", 
                    messageId = message.Id 
                });
            }
            catch (KeyNotFoundException)
            {
                return BadRequest(new { success = false, error = "Invalid JSON structure: Missing required properties." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpGet("recent-chats")]
        public async Task<IActionResult> GetRecentChats()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                // Step 1: Get all distinct user IDs that the current user has chatted with
                var chatUserIds = await _context.Messages
                    .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                    .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                    .Distinct()
                    .ToListAsync();

                // Step 2: Get basic information about these users
                var chatUsers = await _context.Users
                    .Where(u => chatUserIds.Contains(u.Id))
                    .Select(u => new
                    {
                        u.Id,
                        u.Name,
                        u.LastName,
                        u.ProfilePicturePath
                    })
                    .ToListAsync();

                // Step 3: For each user, get the unread count
                var unreadCounts = await _context.Messages
                    .Where(m => m.ReceiverId == userId && !m.IsRead)
                    .GroupBy(m => m.SenderId)
                    .Select(g => new
                    {
                        SenderId = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(x => x.SenderId, x => x.Count);

                // Step 4: Build result with all needed information
                var result = new List<object>();
                
                foreach (var user in chatUsers)
                {
                    // Step 5: Get the most recent message for this user pair
                    var lastMessage = await _context.Messages
                        .Where(m => (m.SenderId == userId && m.ReceiverId == user.Id) ||
                                     (m.SenderId == user.Id && m.ReceiverId == userId))
                        .OrderByDescending(m => m.Timestamp)
                        .Select(m => new
                        {
                            content = m.IsDeleted ? "Deleted message" : m.Content,
                            isFromUser = m.SenderId == userId,
                            isDeleted = m.IsDeleted,
                            isRead = m.IsRead,
                            timestamp = m.Timestamp
                        })
                        .FirstOrDefaultAsync();

                    // Add complete user info to result
                    result.Add(new
                    {
                        id = user.Id,
                        fullName = user.Name + " " + user.LastName,
                        profilePicturePath = user.ProfilePicturePath ?? "/images/default/default-profile.png",
                        isOnline = _chatHub.IsUserOnline(user.Id),
                        lastMessage = lastMessage,
                        unreadCount = unreadCounts.ContainsKey(user.Id) ? unreadCounts[user.Id] : 0
                    });
                }

                // Sort by last message timestamp
                result = result
                    .OrderByDescending(u => {
                        var lastMessage = u.GetType().GetProperty("lastMessage").GetValue(u, null);
                        if (lastMessage == null) return DateTime.MinValue;
                        return (DateTime)lastMessage.GetType().GetProperty("timestamp").GetValue(lastMessage, null);
                    })
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpPost("mark-as-read/{senderId}")]
        public async Task<IActionResult> MarkMessagesAsRead(string senderId)
        {
            var userId = _userManager.GetUserId(User);

            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == senderId && m.ReceiverId == userId && !m.IsRead)
                .ToListAsync();

            if (!unreadMessages.Any())
                return Ok(new { success = false, message = "No unread messages found" });

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }


        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUser(string userId)
        {
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { 
                    id = u.Id, 
                    fullName = u.Name + " " + u.LastName, 
                    profilePicturePath = u.ProfilePicturePath,
                    isOnline = _chatHub.IsUserOnline(userId)
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            return Ok(user);
        }

        [HttpDelete("messages/delete/{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);

            if (message == null)
                return BadRequest(new { error = "Message not found" });

            message.IsDeleted = true; // ✅ Soft delete the message
            await _context.SaveChangesAsync();

            await _hubContext.Clients.User(message.SenderId).SendAsync("MessageDeleted", messageId);
            await _hubContext.Clients.User(message.ReceiverId).SendAsync("MessageDeleted", messageId);

            return Ok(new { success = true, message = "Message deleted for both users." });
        }

        [HttpPost("messages/delete-for-me/{messageId}")]
        public async Task<IActionResult> MessageDeletedForMe(string messageId)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var message = await _context.Messages.FindAsync(int.Parse(messageId));

            if (message == null)
                return BadRequest(new { error = "Message not found" });

            Console.WriteLine($"✅ [API] Sending delete event for message {messageId} to user {userId}");


            await _hubContext.Clients.User(userId).SendAsync("MessageDeletedForMe", messageId, userId);

            return Ok(new { success = true, message = "Message hidden for you." });
        }

        [HttpPost("groups/create")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { success = false, message = "Group name is required" });

            if (dto.Members == null || dto.Members.Count < 2)
                return BadRequest(new { success = false, message = "At least 2 members are required" });

            var group = new GroupChat
            {
                Name = dto.Name,
                CreatedById = currentUser.Id
            };

            var members = new List<GroupMember>();
            // Add creator as admin
            members.Add(new GroupMember { UserId = currentUser.Id, IsAdmin = true }); 

            foreach (var memberId in dto.Members)
            {
                if (await _context.Users.AnyAsync(u => u.Id == memberId))
                {
                    members.Add(new GroupMember { UserId = memberId, IsAdmin = false });
                }
            }

            group.Members = members;

            _context.GroupChats.Add(group);
            await _context.SaveChangesAsync();

            // Notify members through SignalR
            foreach (var member in members)
            {
                await _hubContext.Clients.User(member.UserId)
                    .SendAsync("GroupCreated", new { group.Id, group.Name });
            }

            return Ok(new { success = true, groupId = group.Id });
        }

        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups()
        {
            var userId = _userManager.GetUserId(User);
            
            var groups = await _context.GroupChats
                .Where(g => g.Members.Any(m => m.UserId == userId))
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    LastMessage = g.Messages
                        .OrderByDescending(m => m.Timestamp)
                        .Select(m => new { 
                            Content = m.IsDeleted ? "Message deleted" : m.Content,
                            SenderId = m.SenderId
                        })
                        .FirstOrDefault(),
                    HasUnreadMessages = g.Messages.Any(m => 
                        !m.ReadBy.Any(r => r.UserId == userId) && 
                        m.SenderId != userId)
                })
                .ToListAsync();

            var result = groups.Select(g => new
            {
                g.Id,
                g.Name,
                LastMessage = g.LastMessage?.Content,
                LastMessageSenderId = g.LastMessage?.SenderId,
                g.HasUnreadMessages
            });

            return Ok(result);
        }

        [HttpGet("groups/{groupId}")]
        public async Task<IActionResult> GetGroupChat(int groupId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var group = await _context.GroupChats
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .Include(g => g.Messages)
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(g => g.Id == groupId && g.Members.Any(m => m.UserId == userId));

            if (group == null)
                return NotFound(new { error = "Group not found" });

            // Get current user's member info
            var currentUserMember = group.Members.FirstOrDefault(m => m.UserId == userId);
            var isCreator = group.CreatedById == userId;
            var isAdmin = currentUserMember?.IsAdmin ?? false;

            // Sort members: creator first, then others alphabetically
            var sortedMembers = group.Members
                .OrderByDescending(m => m.UserId == group.CreatedById) // Creator first
                .ThenBy(m => m.User.Name) // Then by first name
                .ThenBy(m => m.User.LastName) // Then by last name if first names are same
                .Select(m => new
                {
                    m.User.Id,
                    FullName = m.User.Name + " " + m.User.LastName,
                    m.User.ProfilePicturePath,
                    m.IsAdmin,
                    IsCreator = group.CreatedById == m.UserId,
                    // Fix CanManageGroup logic:
                    // - Creator can manage everyone except themselves
                    // - Admins can manage non-admin members
                    CanManageGroup = (isCreator && m.UserId != userId) || // Creator can manage others
                                    (isAdmin && !m.IsAdmin && m.UserId != group.CreatedById && m.UserId != userId), // Admins can manage non-admins except creator and themselves
                    CanManageRoles = group.CreatedById == userId
                }).ToList();

            var result = new
            {
                group.Id,
                group.Name,
                group.CreatedById,
                MemberCount = group.Members.Count,
                Members = sortedMembers,
                CurrentUserRole = new
                {
                    IsAdmin = isAdmin,
                    IsCreator = isCreator,
                    CanManageGroup = isAdmin || isCreator,
                    CanManageRoles = isCreator
                },
                Messages = group.Messages
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new
                    {
                        m.Id,
                        m.SenderId,
                        SenderName = m.Sender.Name + " " + m.Sender.LastName,
                        SenderProfilePicture = m.Sender.ProfilePicturePath,
                        m.Content,
                        m.Timestamp,
                        m.IsDeleted
                    }).ToList()
            };

            return Ok(result);
        }

        [HttpPost("groups/message")]
        public async Task<IActionResult> SendGroupMessage([FromBody] GroupMessageDto message)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            // Verify user is member of the group
            var isMember = await _context.GroupMembers
                .AnyAsync(m => m.GroupId == message.GroupId && m.UserId == userId);

            if (!isMember)
                return BadRequest(new { error = "You are not a member of this group" });

            var groupMessage = new GroupMessage
            {
                GroupId = message.GroupId,
                SenderId = userId,
                Content = message.Content
            };

            _context.GroupMessages.Add(groupMessage);
            await _context.SaveChangesAsync();

            // Get the sender's full name
            var sender = await _userManager.FindByIdAsync(userId);
            var senderName = $"{sender.Name} {sender.LastName}";

            // Get all group members
            var groupMembers = await _context.GroupMembers
                .Where(m => m.GroupId == message.GroupId)
                .Select(m => m.UserId)
                .ToListAsync();

            // Notify all group members through SignalR
            foreach (var memberId in groupMembers)
            {
                await _hubContext.Clients.User(memberId).SendAsync("ReceiveGroupMessage", new
                {
                    groupId = message.GroupId,
                    messageId = groupMessage.Id,
                    senderId = userId,
                    senderName = senderName,
                    content = message.Content,
                    timestamp = groupMessage.Timestamp
                });
            }

            return Ok(new { success = true });
        }

        [HttpPost("groups/{groupId}/read")]
        public async Task<IActionResult> MarkGroupMessagesAsRead(int groupId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var unreadMessages = await _context.GroupMessages
                .Where(m => 
                    m.GroupId == groupId && 
                    !m.ReadBy.Any(r => r.UserId == userId) &&
                    m.SenderId != userId)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                _context.GroupMessageReads.Add(new GroupMessageRead
                {
                    MessageId = message.Id,
                    UserId = userId
                });
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("groups/{groupId}/name")]
        public async Task<IActionResult> UpdateGroupName(int groupId, [FromBody] UpdateGroupNameDto dto)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var group = await _context.GroupChats
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound(new { error = "Group not found" });

            // Check if current user is creator or admin
            var currentUserMember = group.Members.FirstOrDefault(m => m.UserId == userId);
            if (currentUserMember == null || (!currentUserMember.IsAdmin && group.CreatedById != userId))
                return Unauthorized(new { error = "Only group creator and admins can update group name" });

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { error = "Group name cannot be empty" });

            group.Name = dto.Name;
            await _context.SaveChangesAsync();

            // Notify all group members about the name change
            var groupMembers = group.Members.Select(m => m.UserId).ToList();
            foreach (var memberId in groupMembers)
            {
                await _hubContext.Clients.User(memberId)
                    .SendAsync("GroupNameUpdated", groupId, dto.Name);
            }

            return Ok(new { success = true });
        }

        [HttpDelete("groups/{groupId}/members/{memberId}")]
        public async Task<IActionResult> RemoveGroupMember(int groupId, string memberId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var group = await _context.GroupChats
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound(new { error = "Group not found" });

            // Check if current user is creator or admin
            var currentUserMember = group.Members.FirstOrDefault(m => m.UserId == userId);
            if (currentUserMember == null || (!currentUserMember.IsAdmin && group.CreatedById != userId))
                return Unauthorized(new { error = "Only group creator and admins can remove members" });

            // Prevent removing the creator
            if (memberId == group.CreatedById)
                return BadRequest(new { error = "Cannot remove the group creator" });

            // Prevent non-creator admins from removing other admins
            var memberToRemove = group.Members.FirstOrDefault(m => m.UserId == memberId);
            if (memberToRemove == null)
                return NotFound(new { error = "Member not found in group" });

            if (memberToRemove.IsAdmin && group.CreatedById != userId)
                return Unauthorized(new { error = "Only the group creator can remove admins" });

            group.Members.Remove(memberToRemove);
            await _context.SaveChangesAsync();

            // Notify the removed member and other group members
            await _hubContext.Clients.User(memberId)
                .SendAsync("RemovedFromGroup", groupId);

            var remainingMembers = group.Members.Select(m => m.UserId).ToList();
            foreach (var member in remainingMembers)
            {
                await _hubContext.Clients.User(member)
                    .SendAsync("GroupMemberRemoved", groupId, memberId);
            }

            return Ok(new { success = true });
        }

        [HttpPost("groups/{groupId}/members")]
        public async Task<IActionResult> AddGroupMember(int groupId, [FromBody] AddMemberDto dto)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var group = await _context.GroupChats
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound(new { error = "Group not found" });

            // Check if current user is creator or admin
            var currentUserMember = group.Members.FirstOrDefault(m => m.UserId == userId);
            if (currentUserMember == null || (!currentUserMember.IsAdmin && group.CreatedById != userId))
                return Unauthorized(new { error = "Only group creator and admins can add members" });

            if (group.Members.Any(m => m.UserId == dto.UserId))
                return BadRequest(new { error = "User is already a member of this group" });

            var newMember = new GroupMember { GroupId = groupId, UserId = dto.UserId, IsAdmin = false };
            group.Members.Add(newMember);
            await _context.SaveChangesAsync();

            // Notify the new member and existing members
            await _hubContext.Clients.User(dto.UserId)
                .SendAsync("AddedToGroup", new { group.Id, group.Name });

            foreach (var member in group.Members.Where(m => m.UserId != dto.UserId))
            {
                await _hubContext.Clients.User(member.UserId)
                    .SendAsync("GroupMemberAdded", groupId, dto.UserId);
            }

            return Ok(new { success = true });
        }

        [HttpPost("groups/{groupId}/admins/{userId}")]
        public async Task<IActionResult> AddGroupAdmin(int groupId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
                return Unauthorized();

            try
            {
                Console.WriteLine($"Starting AddGroupAdmin for groupId: {groupId}, userId: {userId}");

                // Find the group and include members in a single query
                var group = await _context.GroupChats
                    .Include(g => g.Members)
                    .FirstOrDefaultAsync(g => g.Id == groupId);

                if (group == null)
                {
                    Console.WriteLine("Group not found");
                    return NotFound(new { error = "Group not found" });
                }

                // Check if the current user is the creator
                if (group.CreatedById != currentUserId)
                {
                    Console.WriteLine("User not authorized to manage admin roles");
                    return Unauthorized(new { error = "Only group creator can manage admin roles" });
                }

                // Find the member to make admin from the already loaded members
                var targetMember = group.Members.FirstOrDefault(m => m.UserId == userId);

                if (targetMember == null)
                {
                    Console.WriteLine("Target member not found");
                    return NotFound(new { error = "User is not a member of the group" });
                }

                // Don't allow modifying the creator's admin status
                if (userId == group.CreatedById)
                {
                    Console.WriteLine("Attempted to modify creator's admin status");
                    return BadRequest(new { error = "Cannot modify the creator's admin status" });
                }

                Console.WriteLine($"Current IsAdmin status: {targetMember.IsAdmin}");
                Console.WriteLine("Attempting to update admin status...");

                // Update the admin status
                targetMember.IsAdmin = true;
                _context.Entry(targetMember).State = EntityState.Modified;
                
                // Save changes
                var saveResult = await _context.SaveChangesAsync();
                Console.WriteLine($"SaveChanges result: {saveResult} changes saved");

                // Verify the change immediately after saving
                var verifyMember = await _context.GroupMembers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

                Console.WriteLine($"Verification check - IsAdmin status: {verifyMember?.IsAdmin}");

                if (verifyMember == null || !verifyMember.IsAdmin)
                {
                    throw new Exception("Failed to verify admin status update");
                }

                // Notify all group members about the admin role change
                foreach (var member in group.Members)
                {
                    await _hubContext.Clients.User(member.UserId)
                        .SendAsync("AdminRoleUpdated", new
                        {
                            groupId,
                            userId,
                            isAdmin = true,
                            canManageGroup = true
                        });
                }

                Console.WriteLine("Successfully updated admin status");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddGroupAdmin: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = $"Failed to update admin role: {ex.Message}" });
            }
        }

        [HttpDelete("groups/{groupId}/admins/{userId}")]
        public async Task<IActionResult> RemoveGroupAdmin(int groupId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
                return Unauthorized();

            try
            {
                // Find the group and include members
                var group = await _context.GroupChats
                    .Include(g => g.Members)
                    .FirstOrDefaultAsync(g => g.Id == groupId);

                if (group == null)
                    return NotFound(new { error = "Group not found" });

                // Check if the current user is the creator
                if (group.CreatedById != currentUserId)
                    return Unauthorized(new { error = "Only group creator can remove admins" });

                // Find the member to remove admin status
                var targetMember = await _context.GroupMembers
                    .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

                if (targetMember == null)
                    return NotFound(new { error = "User is not a member of the group" });

                // Don't allow modifying the creator's admin status
                if (userId == group.CreatedById)
                    return BadRequest(new { error = "Cannot modify the creator's admin status" });

                // Update admin status
                targetMember.IsAdmin = false;
                _context.GroupMembers.Update(targetMember);
                await _context.SaveChangesAsync();

                // Notify all group members about the admin role change
                var groupMembers = group.Members.Select(m => m.UserId).ToList();
                foreach (var memberId in groupMembers)
                {
                    await _hubContext.Clients.User(memberId)
                        .SendAsync("AdminRoleUpdated", groupId, userId, false);
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to update admin role: {ex.Message}" });
            }
        }
    }

    public class UpdateGroupNameDto
    {
        public string Name { get; set; }
    }

    public class AddMemberDto
    {
        public string UserId { get; set; }
    }
}
