//using System;
//using System.Net;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Text;
//using System.Threading.Tasks;
//using System.Text.Json;
//using Xunit;
//using Microsoft.AspNetCore.Mvc.Testing;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using Managly.Models;
//using Managly.Data;
//using Managly.Models.DTOs.ClockIn;
//using Managlytest.Helpers;
//using System.Collections.Generic;

//namespace Managlytest.Integration
//{
//    public class ClockInIntegrationTests : IClassFixture<ClockInTestWebApplicationFactory<Managly.Program>>
//    {
//        private readonly HttpClient _client;
//        private readonly ClockInTestWebApplicationFactory<Managly.Program> _factory;
//        private readonly string _testUserId = "test-integration-user";
//        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
//        {
//            PropertyNameCaseInsensitive = true
//        };

//        public ClockInIntegrationTests(ClockInTestWebApplicationFactory<Managly.Program> factory)
//        {
//            _factory = factory;
//            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
//            {
//                AllowAutoRedirect = false
//            });

//            // Set up the database with test user here, if not already done in the factory
//            using (var scope = _factory.Services.CreateScope())
//            {
//                var services = scope.ServiceProvider;
//                var context = services.GetRequiredService<ApplicationDbContext>();

//                // Check if UserManager is available, as it might not be in integration tests
//                var userManager = services.GetService<UserManager<User>>();
//                if (userManager != null)
//                {
//                    SeedTestData(context, userManager).Wait();
//                }
//                else
//                {
//                    // Fallback if UserManager isn't available
//                    SeedTestDataWithoutUserManager(context).Wait();
//                }
//            }
//        }

//        // Add a fallback method for seeding when UserManager isn't available
//        private async Task SeedTestDataWithoutUserManager(ApplicationDbContext context)
//        {
//            // Check if test user exists
//            if (!await context.Users.AnyAsync(u => u.Id == _testUserId))
//            {
//                // Create test user directly in the database
//                context.Users.Add(new User
//                {
//                    Id = _testUserId,
//                    UserName = "test@example.com",
//                    Email = "test@example.com",
//                    Name = "Test",
//                    LastName = "Integration",
//                    CompanyId = 1,
//                    Address = "Test Address",
//                    City = "Test City",
//                    Country = "Test Country"
//                });
//            }

//            // Add a company if it doesn't exist
//            if (!await context.Companies.AnyAsync())
//            {
//                context.Companies.Add(new Company
//                {
//                    Id = 1,
//                    Name = "Test Company",
//                });
//            }

//            await context.SaveChangesAsync();
//        }

//        private async Task SeedTestData(ApplicationDbContext context, UserManager<User> userManager)
//        {
//            // Check if test data already exists
//            if (await userManager.FindByIdAsync(_testUserId) == null)
//            {
//                // Create test user
//                var user = new User
//                {
//                    Id = _testUserId,
//                    UserName = "test@example.com",
//                    Email = "test@example.com",
//                    Name = "Test",
//                    LastName = "Integration",
//                    CompanyId = 1,
//                    Address = "Test Address",
//                    City = "Test City",
//                    Country = "Test Country"
//                };

//                await userManager.CreateAsync(user, "Test123!");
//                await userManager.AddToRoleAsync(user, "Admin");
//            }

//            // Add a company if it doesn't exist
//            if (!await context.Companies.AnyAsync())
//            {
//                context.Companies.Add(new Company
//                {
//                    Id = 1,
//                    Name = "Test Company",
//                });
//                await context.SaveChangesAsync();
//            }
//        }

//        private async Task<string> GetAuthTokenAsync(string username, string password)
//        {
//            // This depends on your authentication system
//            // For JWT-based authentication, you would typically:
//            var tokenRequest = new
//            {
//                Username = username,
//                Password = password
//            };

//            var content = new StringContent(
//                JsonSerializer.Serialize(tokenRequest),
//                Encoding.UTF8,
//                "application/json");

//            var response = await _client.PostAsync("/api/auth/login", content);
//            response.EnsureSuccessStatusCode();

//            var responseContent = await response.Content.ReadAsStringAsync();
//            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, _jsonOptions);
            
//            return tokenResponse.Token;
//        }

//        private async Task<HttpClient> GetAuthenticatedClientAsync()
//        {
//            try
//            {
//                // Get auth token
//                var token = await GetAuthTokenAsync("test@example.com", "Test123!");
                
//                var client = _factory.CreateClient();
//                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                
//                return client;
//            }
//            catch
//            {
//                // For tests where we can't get a token, simulate authentication
//                // This is a fallback approach if your auth system isn't easily testable
//                using (var scope = _factory.Services.CreateScope())
//                {
//                    // Create a client with authenticated user
//                    var client = _factory.CreateClient();
                    
//                    // Add authentication cookie or header based on your auth mechanism
//                    // This will depend on your specific authentication setup
                    
//                    return client;
//                }
//            }
//        }

//        [Fact]
//        public async Task Unauthorized_User_Cannot_Access_ClockIn_Endpoint()
//        {
//            // Act
//            var response = await _client.PostAsync("/api/attendance/clock-in", null);

//            // Assert
//            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
//        }

//        [Fact]
//        public async Task ClockIn_Returns_Successful_Response_For_Authenticated_User()
//        {
//            // Arrange
//            var client = await GetAuthenticatedClientAsync();

//            // Ensure no active session exists
//            using (var scope = _factory.Services.CreateScope())
//            {
//                var services = scope.ServiceProvider;
//                var context = services.GetRequiredService<ApplicationDbContext>();
                
//                // Remove any active sessions
//                var activeSessions = await context.Attendances
//                    .Where(a => a.UserId == _testUserId && a.CheckOutTime == null)
//                    .ToListAsync();
                    
//                context.Attendances.RemoveRange(activeSessions);
//                await context.SaveChangesAsync();
//            }

//            // Act
//            var response = await client.PostAsync("/api/attendance/clock-in", null);

//            // Assert
//            response.EnsureSuccessStatusCode();
            
//            var content = await response.Content.ReadAsStringAsync();
//            var clockInResponse = JsonSerializer.Deserialize<ClockInResponseDto>(content, _jsonOptions);
            
//            Assert.True(clockInResponse.Success);
//            Assert.True(DateTime.Now.AddMinutes(-5) <= clockInResponse.CheckInTime &&
//                       clockInResponse.CheckInTime <= DateTime.Now.AddMinutes(5));
//        }

//        [Fact]
//        public async Task ClockIn_Returns_BadRequest_When_Already_Clocked_In()
//        {
//            // Arrange
//            var client = await GetAuthenticatedClientAsync();
            
//            // Ensure there's an active session
//            using (var scope = _factory.Services.CreateScope())
//            {
//                var services = scope.ServiceProvider;
//                var context = services.GetRequiredService<ApplicationDbContext>();
                
//                // Remove any active sessions first to ensure clean state
//                var activeSessions = await context.Attendances
//                    .Where(a => a.UserId == _testUserId && a.CheckOutTime == null)
//                    .ToListAsync();
                
//                context.Attendances.RemoveRange(activeSessions);
                
//                // Add a new active session
//                context.Attendances.Add(new Attendance
//                {
//                    UserId = _testUserId,
//                    CheckInTime = DateTime.Now.AddHours(-1),
//                    CheckOutTime = null
//                });
                
//                await context.SaveChangesAsync();
//            }

//            // Act - attempt to clock in again
//            var response = await client.PostAsync("/api/attendance/clock-in", null);

//            // Assert
//            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            
//            var content = await response.Content.ReadAsStringAsync();
//            var errorResponse = JsonSerializer.Deserialize<ApiResponseDto>(content, _jsonOptions);
            
//            Assert.False(errorResponse.Success);
//            Assert.Equal("You are already clocked in", errorResponse.Error);
//        }

//        [Fact]
//        public async Task ClockOut_Returns_Successful_Response_For_Active_Session()
//        {
//            // Arrange
//            var client = await GetAuthenticatedClientAsync();
            
//            // Ensure there's an active session
//            using (var scope = _factory.Services.CreateScope())
//            {
//                var services = scope.ServiceProvider;
//                var context = services.GetRequiredService<ApplicationDbContext>();
                
//                // Remove any active sessions first
//                var activeSessions = await context.Attendances
//                    .Where(a => a.UserId == _testUserId && a.CheckOutTime == null)
//                    .ToListAsync();
                
//                context.Attendances.RemoveRange(activeSessions);
                
//                // Add a new active session
//                context.Attendances.Add(new Attendance
//                {
//                    UserId = _testUserId,
//                    CheckInTime = DateTime.Now.AddHours(-2),
//                    CheckOutTime = null
//                });
                
//                await context.SaveChangesAsync();
//            }

//            // Act
//            var response = await client.PostAsync("/api/attendance/clock-out", null);

//            // Assert
//            response.EnsureSuccessStatusCode();
            
//            var content = await response.Content.ReadAsStringAsync();
//            var clockOutResponse = JsonSerializer.Deserialize<ClockOutResponseDto>(content, _jsonOptions);
            
//            Assert.True(clockOutResponse.Success);
//            Assert.NotNull(clockOutResponse.CheckOutTime);
//            Assert.True(clockOutResponse.Duration > 1.9 && clockOutResponse.Duration < 2.1);
//        }

//        [Fact]
//        public async Task ClockOut_Returns_BadRequest_When_No_Active_Session()
//        {
//            // Arrange
//            var client = await GetAuthenticatedClientAsync();
            
//            // Ensure there's no active session
//            using (var scope = _factory.Services.CreateScope())
//            {
//                var services = scope.ServiceProvider;
//                var context = services.GetRequiredService<ApplicationDbContext>();
                
//                // Remove any active sessions
//                var activeSessions = await context.Attendances
//                    .Where(a => a.UserId == _testUserId && a.CheckOutTime == null)
//                    .ToListAsync();
                
//                context.Attendances.RemoveRange(activeSessions);
//                await context.SaveChangesAsync();
//            }

//            // Act
//            var response = await client.PostAsync("/api/attendance/clock-out", null);

//            // Assert
//            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            
//            var content = await response.Content.ReadAsStringAsync();
//            var errorResponse = JsonSerializer.Deserialize<ApiResponseDto>(content, _jsonOptions);
            
//            Assert.False(errorResponse.Success);
//            Assert.Equal("No active session found", errorResponse.Error);
//        }

//        [Fact]
//        public async Task GetCurrentSession_Returns_Correct_Status_When_Active()
//        {
//            // Arrange
//            var client = await GetAuthenticatedClientAsync();
//            var checkInTime = DateTime.Now.AddHours(-1);
            
//            // Ensure there's an active session
//            using (var scope = _factory.Services.CreateScope())
//            {
//                var services = scope.ServiceProvider;
//                var context = services.GetRequiredService<ApplicationDbContext>();
                
//                // Remove any active sessions first
//                var activeSessions = await context.Attendances
//                    .Where(a => a.UserId == _testUserId && a.CheckOutTime == null)
//                    .ToListAsync();
                
//                context.Attendances.RemoveRange(activeSessions);
                
//                // Add a new active session
//                context.Attendances.Add(new Attendance
//                {
//                    UserId = _testUserId,
//                    CheckInTime = checkInTime,
//                    CheckOutTime = null
//                });
                
//                await context.SaveChangesAsync();
//            }

//            // Act
//            var response = await client.GetAsync("/api/attendance/current-session");

//            // Assert
//            response.EnsureSuccessStatusCode();
            
//            var content = await response.Content.ReadAsStringAsync();
//            var sessionStatus = JsonSerializer.Deserialize<SessionStatusDto>(content, _jsonOptions);
            
//            Assert.True(sessionStatus.Active);
//            Assert.Equal(checkInTime, sessionStatus.CheckInTime);
//            Assert.True(sessionStatus.ElapsedTime > 3500 && sessionStatus.ElapsedTime < 3700); // ~1 hour in seconds
//        }

//        [Fact]
//        public async Task GetCurrentSession_Returns_Inactive_Status_When_No_Active_Session()
//        {
//            // Arrange
//            var client = await GetAuthenticatedClientAsync();
            
//            // Ensure there's no active session
//            using (var scope = _factory.Services.CreateScope())
//            {
//                var services = scope.ServiceProvider;
//                var context = services.GetRequiredService<ApplicationDbContext>();
                
//                // Remove any active sessions
//                var activeSessions = await context.Attendances
//                    .Where(a => a.UserId == _testUserId && a.CheckOutTime == null)
//                    .ToListAsync();
                
//                context.Attendances.RemoveRange(activeSessions);
//                await context.SaveChangesAsync();
//            }

//            // Act
//            var response = await client.GetAsync("/api/attendance/current-session");

//            // Assert
//            response.EnsureSuccessStatusCode();
            
//            var content = await response.Content.ReadAsStringAsync();
//            var sessionStatus = JsonSerializer.Deserialize<SessionStatusDto>(content, _jsonOptions);
            
//            Assert.False(sessionStatus.Active);
//            Assert.Null(sessionStatus.CheckInTime);
//        }

//        [Fact]
//        public async Task GetWorkHistory_Returns_Empty_Message_When_No_History()
//        {
//            // Arrange
//            var client = await GetAuthenticatedClientAsync();
            
//            // Ensure there's no attendance history
//            using (var scope = _factory.Services.CreateScope())
//            {
//                var services = scope.ServiceProvider;
//                var context = services.GetRequiredService<ApplicationDbContext>();
                
//                // Remove all attendance records
//                var allRecords = await context.Attendances
//                    .Where(a => a.UserId == _testUserId)
//                    .ToListAsync();
                
//                context.Attendances.RemoveRange(allRecords);
//                await context.SaveChangesAsync();
//            }

//            // Act
//            var response = await client.GetAsync("/api/attendance/work-history");

//            // Assert
//            response.EnsureSuccessStatusCode();
            
//            var content = await response.Content.ReadAsStringAsync();
//            // Parse the message from the anonymous object
//            using (JsonDocument doc = JsonDocument.Parse(content))
//            {
//                JsonElement root = doc.RootElement;
//                if (root.TryGetProperty("message", out JsonElement messageElement))
//                {
//                    string message = messageElement.GetString();
//                    Assert.Equal("No records found.", message);
//                }
//                else
//                {
//                    Assert.Fail("Response does not contain a 'message' property");
//                }
//            }
//        }

//        [Fact]
//        public async Task GetWorkHistory_Returns_Records_When_History_Exists()
//        {
//            // Arrange
//            var client = await GetAuthenticatedClientAsync();
//            var now = DateTime.Now;
            
//            // Ensure there's attendance history
//            using (var scope = _factory.Services.CreateScope())
//            {
//                var services = scope.ServiceProvider;
//                var context = services.GetRequiredService<ApplicationDbContext>();
                
//                // Remove all attendance records first
//                var allRecords = await context.Attendances
//                    .Where(a => a.UserId == _testUserId)
//                    .ToListAsync();
                
//                context.Attendances.RemoveRange(allRecords);
                
//                // Add some attendance records
//                context.Attendances.Add(new Attendance
//                {
//                    UserId = _testUserId,
//                    CheckInTime = now.AddDays(-1),
//                    CheckOutTime = now.AddDays(-1).AddHours(8)
//                });
                
//                context.Attendances.Add(new Attendance
//                {
//                    UserId = _testUserId,
//                    CheckInTime = now.AddDays(-2),
//                    CheckOutTime = now.AddDays(-2).AddHours(7)
//                });
                
//                await context.SaveChangesAsync();
//            }

//            // Act
//            var response = await client.GetAsync("/api/attendance/work-history");

//            // Assert
//            response.EnsureSuccessStatusCode();
            
//            var content = await response.Content.ReadAsStringAsync();
//            var historyEntries = JsonSerializer.Deserialize<List<WorkHistoryEntryDto>>(content, _jsonOptions);
            
//            Assert.Equal(2, historyEntries.Count);
//            // First entry should be the most recent one
//            Assert.Equal(now.AddDays(-1).Date, historyEntries[0].CheckInTime.Date);
//        }

//        // Helper class for auth token response
//        private class TokenResponse
//        {
//            public string Token { get; set; }
//        }
//    }
//} 