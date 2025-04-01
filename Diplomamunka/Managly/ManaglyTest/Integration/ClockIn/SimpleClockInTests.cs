using System.Net;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Managlytest.Helpers;

namespace Managlytest.Integration
{
    public class SimpleClockInTests : IClassFixture<ClockInTestWebApplicationFactory<Managly.Program>>
    {
        private readonly ClockInTestWebApplicationFactory<Managly.Program> _factory;

        public SimpleClockInTests(ClockInTestWebApplicationFactory<Managly.Program> factory)
        {
            _factory = factory;
        }

        //[Fact]
        //public async Task Unauthenticated_Request_Returns_Unauthorized()
        //{
        //    // Arrange
        //    var client = _factory.CreateClient();

        //    // Act
        //    var response = await client.PostAsync("/api/attendance/clock-in", null);

        //    // Assert
        //    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        //}
    }
} 