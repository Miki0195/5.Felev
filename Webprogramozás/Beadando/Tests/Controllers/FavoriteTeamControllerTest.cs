using Xunit;
using Microsoft.AspNetCore.Mvc;
using Beadando.Controllers;
using Beadando.Models;
using Beadando.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

public class FavoriteTeamControllerTests
{
    private readonly SportsDbContext _context;
    private readonly FavoriteTeamController _controller;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

    public FavoriteTeamControllerTests()
    {
        var options = new DbContextOptionsBuilder<SportsDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        _context = new SportsDbContext(options);
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

        _controller = new FavoriteTeamController(_context, _userManagerMock.Object);

        // Reset database before each test
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
    }

    [Fact]
    public void TeamsByLeague_ValidLeagueId_ShouldReturnPartialViewWithTeams()
    {
        // Arrange
        _context.Leagues.Add(new League { Id = 99, Name = "League A" });
        _context.Teams.Add(new Team { Id = 99, Name = "Team A", LeagueId = 99 });
        _context.SaveChanges();

        // Act
        var result = _controller.TeamsByLeague(99) as PartialViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("~/Views/FavoriteTeam/_TeamsList.cshtml", result.ViewName);
        var teams = result.Model as List<Team>;
        Assert.Single(teams);
    }

    [Fact]
    public void TeamsByLeague_InvalidLeagueId_ShouldReturnBadRequest()
    {
        // Act
        var result = _controller.TeamsByLeague(-1) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }
}
