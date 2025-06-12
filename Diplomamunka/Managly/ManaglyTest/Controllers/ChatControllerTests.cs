using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;
using System.Text.Json;
using Managly.Controllers;
using Managly.Data;
using Managly.Hubs;
using Managly.Models;
using System.Dynamic;

namespace Managly.Tests.Controllers
{
    // Create an interface for ChatHub to facilitate mocking
    public interface IChatHub
    {
        bool IsUserOnline(string userId);
        Task SendMessage(string senderId, string receiverId, string message);
        Task MessageDeleted(string messageId);
        Task DeleteMessage(string messageId, string senderId, string receiverId);
        Task<string> MessageDeletedForMe(string messageId, string userId);
    }

    public class ChatControllerTests
    {
        // Database and dependencies
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<IHubContext<ChatHub>> _mockHubContext;
        private readonly Mock<IChatHub> _mockChatHub;
        
        // Test data
        private readonly User _testUser1;
        private readonly User _testUser2;
        private readonly User _testUser3;
        private readonly List<Message> _testMessages;
        private readonly List<GroupChat> _testGroups;
        private readonly List<GroupMember> _testGroupMembers;
        private readonly List<GroupMessage> _testGroupMessages;
        
        public ChatControllerTests()
        {
            // Initialize test data
            _testUser1 = new User
            {
                Id = "user-id-1",
                UserName = "user1@example.com",
                Email = "user1@example.com",
                Name = "Test",
                LastName = "User1",
                CompanyId = 1,
                ProfilePicturePath = "/images/default/default-profile.png",
                Country = "Test Country",
                City = "Test City",
                Address = "Test Address"
            };
            
            _testUser2 = new User
            {
                Id = "user-id-2",
                UserName = "user2@example.com",
                Email = "user2@example.com",
                Name = "Test",
                LastName = "User2", 
                CompanyId = 1,
                ProfilePicturePath = "/images/default/default-profile.png",
                Country = "Test Country",
                City = "Test City",
                Address = "Test Address"
            };
            
            _testUser3 = new User
            {
                Id = "user-id-3",
                UserName = "user3@example.com",
                Email = "user3@example.com",
                Name = "Test",
                LastName = "User3",
                CompanyId = 2, // Different company
                ProfilePicturePath = "/images/default/default-profile.png",
                Country = "Test Country",
                City = "Test City",
                Address = "Test Address"
            };

            // Create test messages
            _testMessages = new List<Message>
            {
                new Message
                {
                    Id = 1,
                    SenderId = "user-id-1",
                    ReceiverId = "user-id-2",
                    Content = "Hello from User 1",
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    IsRead = true
                },
                new Message
                {
                    Id = 2,
                    SenderId = "user-id-2",
                    ReceiverId = "user-id-1",
                    Content = "Hello back from User 2",
                    Timestamp = DateTime.UtcNow.AddHours(-12),
                    IsRead = false
                },
                new Message
                {
                    Id = 3,
                    SenderId = "user-id-1",
                    ReceiverId = "user-id-2",
                    Content = "How are you?",
                    Timestamp = DateTime.UtcNow.AddHours(-6),
                    IsRead = false
                },
                new Message
                {
                    Id = 4,
                    SenderId = "user-id-1",
                    ReceiverId = "user-id-3",
                    Content = "Hello User 3",
                    Timestamp = DateTime.UtcNow.AddDays(-2),
                    IsRead = true
                }
            };

            // Create test groups
            _testGroups = new List<GroupChat>
            {
                new GroupChat
                {
                    Id = 1,
                    Name = "Test Group 1",
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    CreatedById = "user-id-1"
                },
                new GroupChat
                {
                    Id = 2,
                    Name = "Test Group 2",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    CreatedById = "user-id-2"
                }
            };

            // Create test group members
            _testGroupMembers = new List<GroupMember>
            {
                new GroupMember
                {
                    GroupId = 1,
                    UserId = "user-id-1",
                    IsAdmin = true,
                    JoinedAt = DateTime.UtcNow.AddDays(-5)
                },
                new GroupMember
                {
                    GroupId = 1,
                    UserId = "user-id-2",
                    IsAdmin = false,
                    JoinedAt = DateTime.UtcNow.AddDays(-5)
                },
                new GroupMember
                {
                    GroupId = 2,
                    UserId = "user-id-1",
                    IsAdmin = false,
                    JoinedAt = DateTime.UtcNow.AddDays(-3)
                },
                new GroupMember
                {
                    GroupId = 2,
                    UserId = "user-id-2",
                    IsAdmin = true,
                    JoinedAt = DateTime.UtcNow.AddDays(-3)
                }
            };

            // Create test group messages
            _testGroupMessages = new List<GroupMessage>
            {
                new GroupMessage
                {
                    Id = 1,
                    GroupId = 1,
                    SenderId = "user-id-1",
                    Content = "First group message",
                    Timestamp = DateTime.UtcNow.AddDays(-4),
                    ReadBy = new List<GroupMessageRead>()
                },
                new GroupMessage
                {
                    Id = 2,
                    GroupId = 1,
                    SenderId = "user-id-2",
                    Content = "Reply to group",
                    Timestamp = DateTime.UtcNow.AddDays(-3),
                    ReadBy = new List<GroupMessageRead>
                    {
                        new GroupMessageRead { MessageId = 2, UserId = "user-id-1" }
                    }
                }
            };

            // Set up in-memory database for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .EnableSensitiveDataLogging()
                .Options;

            _context = new ApplicationDbContext(options);

            // Add users first to avoid foreign key constraints
            _context.Users.AddRange(_testUser1, _testUser2, _testUser3);
            _context.SaveChanges();

            // Add messages
            _context.Messages.AddRange(_testMessages);
            _context.SaveChanges();

            // Add groups
            _context.GroupChats.AddRange(_testGroups);
            _context.SaveChanges();

            // Add group members
            _context.GroupMembers.AddRange(_testGroupMembers);
            _context.SaveChanges();

            // Add group messages
            _context.GroupMessages.AddRange(_testGroupMessages);
            _context.SaveChanges();

            // Mock UserManager
            var userStoreMock = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testUser1);

            _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns("user-id-1");

            _mockUserManager.Setup(m => m.FindByIdAsync("user-id-1"))
                .ReturnsAsync(_testUser1);

            _mockUserManager.Setup(m => m.FindByIdAsync("user-id-2"))
                .ReturnsAsync(_testUser2);

            _mockUserManager.Setup(m => m.FindByIdAsync("user-id-3"))
                .ReturnsAsync(_testUser3);

            // Mock Hub Context
            _mockHubContext = new Mock<IHubContext<ChatHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();

            mockClients.Setup(clients => clients.User(It.IsAny<string>()))
                .Returns(mockClientProxy.Object);

            _mockHubContext.Setup(x => x.Clients)
                .Returns(mockClients.Object);

            // Mock IChatHub instead of ChatHub directly
            _mockChatHub = new Mock<IChatHub>();
            _mockChatHub.Setup(hub => hub.IsUserOnline(It.IsAny<string>()))
                .Returns((string userId) => userId == "user-id-1" || userId == "user-id-2");
        }

        #region Message Tests

        [Fact]
        public async Task GetMessages_ReturnsOnlyMessagesForSpecifiedUsers()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.GetMessages("user-id-2");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var messages = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            
            // Should return all messages between user-id-1 and user-id-2 only
            Assert.Equal(3, messages.Count());
        }

        [Fact]
        public async Task SaveMessage_ValidData_SavesMessageAndReturnsSuccess()
        {
            // Arrange
            var controller = CreateController();
            var initialCount = _context.Messages.Count();
            
            var jsonData = JsonDocument.Parse(@"{
                ""senderId"": ""user-id-1"", 
                ""receiverId"": ""user-id-2"", 
                ""content"": ""New test message""
            }").RootElement;

            // Act
            var result = await controller.SaveMessage(jsonData, _mockHubContext.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var messageObj = okResult.Value;
            
            // Extract properties dynamically 
            var success = GetDynamicProperty<bool>(messageObj, "success");
            var messageId = GetDynamicProperty<int>(messageObj, "messageId");

            Assert.True(success);
            Assert.NotEqual(0, messageId);
            
            // Verify message was saved to database
            Assert.Equal(initialCount + 1, _context.Messages.Count());
            
            // Verify the message content was saved correctly
            var savedMessage = await _context.Messages.OrderByDescending(m => m.Id).FirstOrDefaultAsync();
            Assert.Equal("New test message", savedMessage.Content);
            Assert.Equal("user-id-1", savedMessage.SenderId);
            Assert.Equal("user-id-2", savedMessage.ReceiverId);
        }

        [Fact]
        public async Task SaveMessage_MissingRequiredFields_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            var initialCount = _context.Messages.Count();
            
            var jsonData = JsonDocument.Parse(@"{
                ""senderId"": ""user-id-1"", 
                ""content"": ""Missing receiver""
            }").RootElement;

            // Act
            var result = await controller.SaveMessage(jsonData, _mockHubContext.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObj = badRequestResult.Value;
            
            // Extract error property
            var error = GetDynamicProperty<string>(errorObj, "error");
            Assert.NotNull(error);
            
            // Verify no message was saved
            Assert.Equal(initialCount, _context.Messages.Count());
        }

        [Fact]
        public async Task DeleteMessage_ExistingMessage_SoftDeletesMessage()
        {
            // Arrange
            var controller = CreateController();
            var messageId = 1; // Existing message ID from test data
            
            // Act
            var result = await controller.DeleteMessage(messageId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            // Verify message is marked as deleted
            var message = await _context.Messages.FindAsync(messageId);
            Assert.True(message.IsDeleted);
            
            // Verify the message still exists in the database (soft delete)
            Assert.NotNull(message);
        }

        [Fact]
        public async Task MarkMessagesAsRead_UpdatesUnreadMessagesStatus()
        {
            // Arrange
            var controller = CreateController();
            
            // Act
            var result = await controller.MarkMessagesAsRead("user-id-2");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            // Verify all messages from user-id-2 to user-id-1 are marked as read
            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == "user-id-2" && m.ReceiverId == "user-id-1" && !m.IsRead)
                .ToListAsync();
                
            Assert.Empty(unreadMessages);
        }

        [Fact]
        public async Task GetRecentChats_ReturnsChatsWithUnreadCounts()
        {
            // Arrange
            var controller = CreateController();
            
            // Act
            var result = await controller.GetRecentChats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var chats = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            
            // Should return users that current user has chatted with
            Assert.Equal(2, chats.Count());
            
            // Convert to list to get first item
            var chatsList = chats.ToList();
            
            // Access properties via reflection
            var firstChatProps = chatsList[0].GetType().GetProperties();
            
            // Check that unreadCount property exists
            var unreadCountProp = firstChatProps.FirstOrDefault(p => p.Name == "unreadCount");
            Assert.NotNull(unreadCountProp);
        }

        #endregion

        #region Group Chat Tests

        [Fact]
        public async Task CreateGroup_ValidData_CreatesGroupAndReturnsSuccess()
        {
            // Arrange
            var controller = CreateController();
            var initialGroupCount = _context.GroupChats.Count();
            
            var createGroupDto = new CreateGroupDto
            {
                Name = "New Test Group",
                Members = new List<string> { "user-id-2", "user-id-3" }
            };

            // Act
            var result = await controller.CreateGroup(createGroupDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultObj = okResult.Value;
            
            // Get properties using reflection or dynamic
            var success = GetDynamicProperty<bool>(resultObj, "success");
            var groupId = GetDynamicProperty<int>(resultObj, "groupId");
            
            Assert.True(success);
            Assert.NotEqual(0, groupId);
            
            // Verify group was created in database
            Assert.Equal(initialGroupCount + 1, _context.GroupChats.Count());
            
            // Get the created group
            var createdGroup = await _context.GroupChats
                .Include(g => g.Members)
                .OrderByDescending(g => g.Id)
                .FirstOrDefaultAsync();
                
            Assert.Equal("New Test Group", createdGroup.Name);
            
            // Should include the creator and the members (3 total)
            Assert.Equal(3, createdGroup.Members.Count);
            
            // Creator should be admin
            var creatorMember = createdGroup.Members.FirstOrDefault(m => m.UserId == "user-id-1");
            Assert.True(creatorMember.IsAdmin);
        }

        [Fact]
        public async Task GetGroups_ReturnsOnlyGroupsUserIsMemberOf()
        {
            // Arrange
            var controller = CreateController();
            
            // Create a group where test user is not a member
            var nonMemberGroup = new GroupChat
            {
                Id = 99,
                Name = "Non-Member Group",
                CreatedById = "user-id-3"
            };
            _context.GroupChats.Add(nonMemberGroup);
            
            _context.GroupMembers.Add(new GroupMember
            {
                GroupId = 99,
                UserId = "user-id-3",
                IsAdmin = true
            });
            
            await _context.SaveChangesAsync();
            
            // Act
            var result = await controller.GetGroups();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var groups = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value).ToList();
            
            // Should only return groups where the user is a member
            Assert.Equal(2, groups.Count);
            
            // Convert each group to a dynamic and check ID
            var groupIds = new List<int>();
            foreach (var group in groups)
            {
                var id = GetDynamicProperty<int>(group, "Id");
                groupIds.Add(id);
            }
            
            Assert.Contains(1, groupIds);
            Assert.Contains(2, groupIds);
            Assert.DoesNotContain(99, groupIds);
        }

        [Fact]
        public async Task GetGroupChat_ValidId_ReturnsGroupDetails()
        {
            // Arrange
            var controller = CreateController();
            var groupId = 1; // Existing group ID from test data
            
            // Act
            var result = await controller.GetGroupChat(groupId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var groupDetails = okResult.Value;
            
            // Get properties using reflection
            var id = GetDynamicProperty<int>(groupDetails, "Id");
            var name = GetDynamicProperty<string>(groupDetails, "Name");
            var members = GetDynamicProperty<object>(groupDetails, "Members");
            var messages = GetDynamicProperty<object>(groupDetails, "Messages");
            var currentUserRole = GetDynamicProperty<object>(groupDetails, "CurrentUserRole");
            
            // Verify group details
            Assert.Equal(groupId, id);
            Assert.Equal("Test Group 1", name);
            
            // Verify members are included
            var membersList = (members as IEnumerable<object>)?.ToList();
            Assert.Equal(2, membersList.Count);
            
            // Verify messages are included
            var messagesList = (messages as IEnumerable<object>)?.ToList();
            Assert.Equal(2, messagesList.Count);
            
            // Verify current user role
            var isAdmin = GetDynamicProperty<bool>(currentUserRole, "IsAdmin");
            var isCreator = GetDynamicProperty<bool>(currentUserRole, "IsCreator");
            Assert.True(isAdmin);
            Assert.True(isCreator);
        }

        [Fact]
        public async Task GetGroupChat_NonMemberAccess_ReturnsNotFound()
        {
            // Arrange - Use different user with test setup
            var mockUserManager = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object, null, null, null, null, null, null, null, null);
            
            mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testUser3);
            
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns("user-id-3");
                
            // Create controller with different user setup
            var controller = new ChatController(
                _context,
                mockUserManager.Object,
                _mockHubContext.Object,
                _mockChatHub.Object as ChatHub // Cast our mock interface to ChatHub for controller
            );
            
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-id-3")
            }));
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            
            // Act
            var result = await controller.GetGroupChat(1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task SendGroupMessage_ValidData_SavesMessageAndReturnsSuccess()
        {
            // Arrange
            var controller = CreateController();
            var initialCount = _context.GroupMessages.Count();
            
            var groupMessageDto = new GroupMessageDto
            {
                GroupId = 1,
                Content = "New group test message"
            };

            // Act
            var result = await controller.SendGroupMessage(groupMessageDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultObj = okResult.Value;
            
            // Extract success property
            var success = GetDynamicProperty<bool>(resultObj, "success");
            Assert.True(success);
            
            // Verify message was saved
            Assert.Equal(initialCount + 1, _context.GroupMessages.Count());
            
            // Verify message content
            var savedMessage = await _context.GroupMessages
                .OrderByDescending(m => m.Id)
                .FirstOrDefaultAsync();
                
            Assert.Equal("New group test message", savedMessage.Content);
            Assert.Equal(1, savedMessage.GroupId);
            Assert.Equal("user-id-1", savedMessage.SenderId);
        }

        [Fact]
        public async Task UpdateGroupName_AsAdmin_UpdatesNameAndReturnsSuccess()
        {
            // Arrange
            var controller = CreateController();
            var updateDto = new UpdateGroupNameDto { Name = "Updated Group Name" };
            
            // Act
            var result = await controller.UpdateGroupName(1, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultObj = okResult.Value;
            
            // Extract success property
            var success = GetDynamicProperty<bool>(resultObj, "success");
            Assert.True(success);
            
            // Verify group name was updated
            var updatedGroup = await _context.GroupChats.FindAsync(1);
            Assert.Equal("Updated Group Name", updatedGroup.Name);
        }

        [Fact]
        public async Task UpdateGroupName_AsNonAdmin_ReturnsUnauthorized()
        {
            // Arrange - user is a member but not admin in group 2
            var controller = CreateController();
            var updateDto = new UpdateGroupNameDto { Name = "Attempted Update" };
            
            // Act
            var result = await controller.UpdateGroupName(2, updateDto);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
            
            // Verify group name was not updated
            var group = await _context.GroupChats.FindAsync(2);
            Assert.Equal("Test Group 2", group.Name);
        }

        [Fact]
        public async Task AddGroupMember_AsAdmin_AddsNewMemberAndReturnsSuccess()
        {
            // Arrange
            var controller = CreateController();
            var initialMemberCount = await _context.GroupMembers.CountAsync(gm => gm.GroupId == 1);
            var addMemberDto = new AddMemberDto { UserId = "user-id-3" };
            
            // Act
            var result = await controller.AddGroupMember(1, addMemberDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultObj = okResult.Value;
            
            // Extract success property
            var success = GetDynamicProperty<bool>(resultObj, "success");
            Assert.True(success);
            
            // Verify member was added
            var currentMemberCount = await _context.GroupMembers.CountAsync(gm => gm.GroupId == 1);
            Assert.Equal(initialMemberCount + 1, currentMemberCount);
            
            // Verify the specific user was added
            var memberExists = await _context.GroupMembers.AnyAsync(
                gm => gm.GroupId == 1 && gm.UserId == "user-id-3");
            Assert.True(memberExists);
        }

        #endregion

        #region User Search Tests

        [Fact]
        public async Task SearchUsers_ReturnsMatchingUsersInSameCompany()
        {
            // Arrange
            var controller = CreateController();
            
            // Act
            var result = await controller.SearchUsers("User");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var users = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value).ToList();
            
            // Should only return users in same company (excluding self)
            Assert.Single(users);
            
            // Get ID property from the returned user
            var userId = GetDynamicProperty<string>(users[0], "Id");
            Assert.Equal("user-id-2", userId);
        }

        [Fact]
        public async Task SearchUsers_NoMatches_ReturnsEmptyList()
        {
            // Arrange
            var controller = CreateController();
            
            // Act
            var result = await controller.SearchUsers("NonexistentUser");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var users = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            
            // Should return empty list since no users match
            Assert.Empty(users);
        }

        #endregion

        // Helper method to get property value by name from dynamic object
        private static T GetDynamicProperty<T>(object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property != null)
            {
                return (T)property.GetValue(obj);
            }
            
            // For anonymous types with different casing
            property = obj.GetType().GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));
                
            if (property != null)
            {
                return (T)property.GetValue(obj);
            }
            
            throw new ArgumentException($"Property {propertyName} not found on object of type {obj.GetType().Name}");
        }

        private ChatController CreateController()
        {
            // Create a proxy to wrap the mock IChatHub in a ChatHub for the controller
            var chatHubProxy = new ChatHubTestProxy(_mockChatHub.Object);
            
            var controller = new ChatController(
                _context,
                _mockUserManager.Object,
                _mockHubContext.Object,
                chatHubProxy
            );
            
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-id-1")
            }));
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            
            return controller;
        }
    }

    // Proxy class that inherits from ChatHub but delegates to our IChatHub interface
    public class ChatHubTestProxy : ChatHub
    {
        private readonly IChatHub _chatHub;

        public ChatHubTestProxy(IChatHub chatHub) : base(null) // Pass null for the context as we're not using it
        {
            _chatHub = chatHub;
        }

        // Override the IsUserOnline method to delegate to our mockable interface
        public new bool IsUserOnline(string userId)
        {
            return _chatHub.IsUserOnline(userId);
        }
    }

    
} 