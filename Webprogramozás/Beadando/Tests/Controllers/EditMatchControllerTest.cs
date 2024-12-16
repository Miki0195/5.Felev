using Xunit;
using Microsoft.AspNetCore.Mvc;
using Beadando.Controllers;
using Beadando.Models;
using Beadando.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

public class EditMatchControllerTests
{
    private readonly SportsDbContext _context;
    private readonly EditMatchController _controller;

    public EditMatchControllerTests()
    {
        var options = new DbContextOptionsBuilder<SportsDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        _context = new SportsDbContext(options);
        _controller = new EditMatchController(_context);

        // Reset database before each test
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
    }

    [Fact]
    public void Create_Get_ShouldReturnViewResultWithTeamsAndLeagues()
    {
        // Arrange
        _context.Teams.AddRange(
            new Team { Id = 99, Name = "Team A" },
            new Team { Id = 100, Name = "Team B" }
        );
        _context.Leagues.AddRange(
            new League { Id = 99, Name = "League A" },
            new League { Id = 100, Name = "League B" }
        );
        _context.SaveChanges();

        // Act
        var result = _controller.Create() as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("~/Views/Match/Create.cshtml", result.ViewName);
        Assert.True(result.ViewData.Keys.Contains("Teams"));
        Assert.True(result.ViewData.Keys.Contains("Leagues"));
    }
    /*
    [Fact]
    public void Create_Post_ValidModel_ShouldAddMatchAndRedirect()
    {
        // Arrange
        _context.Teams.AddRange(
            new Team { Id = 99, Name = "Team A" },
            new Team { Id = 100, Name = "Team B" }
        );
        _context.Leagues.Add(new League { Id = 99, Name = "League A" });
        _context.SaveChanges();

        var match = new Match
        {
            HomeTeamId = 99,
            AwayTeamId = 100,
            LeagueId = 99,
            StartTime = System.DateTime.Now
        };

        // Act
        var result = _controller.Create(match) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Create", result.ActionName);
        Assert.Single(_context.Matches);
    }
    */
    [Fact]
    public void Create_Post_InvalidModel_ShouldReturnBadRequest()
    {
        // Arrange
        var match = new Match { HomeTeamId = 99, AwayTeamId = 99, LeagueId = 99 };

        // Act
        var result = _controller.Create(match) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void SearchMatches_ShouldReturnMatchesForGivenTeam()
    {
        // Arrange
        var homeTeam = new Team { Id = 99, Name = "Team A" };
        var awayTeam = new Team { Id = 100, Name = "Team B" };
        var league = new League { Id = 99, Name = "League A" };

        _context.Teams.AddRange(homeTeam, awayTeam);
        _context.Leagues.Add(league);
        _context.Matches.Add(new Match
        {
            Id = 99,
            HomeTeamId = 99,
            AwayTeamId = 100,
            LeagueId = 99,
            HomeTeam = homeTeam,
            AwayTeam = awayTeam,
            StartTime = System.DateTime.Now
        });
        _context.SaveChanges();

        // Act
        var result = _controller.SearchMatches("Team A") as ViewResult;
        var matches = result?.Model as List<MatchViewModel>;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(matches);
        Assert.Single(matches);
        Assert.Equal("Team A", matches.First().HomeTeam);
    }
    /*
    [Fact]
    public void DeleteMatch_ValidId_ShouldDeleteMatchAndRedirect()
    {
        // Arrange
        var match = new Match { Id = 99, HomeTeamId = 99, AwayTeamId = 100, LeagueId = 99 };
        _context.Matches.Add(match);
        _context.SaveChanges();

        // Act
        var result = _controller.DeleteMatch(99) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SearchMatches", result.ActionName);
        Assert.Empty(_context.Matches);
    }
    */
    [Fact]
    public void DeleteMatch_InvalidId_ShouldReturnNotFound()
    {
        // Act
        var result = _controller.DeleteMatch(999) as NotFoundResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public void GetTeamsByLeague_ShouldReturnJsonResult()
    {
        // Arrange
        var league = new League { Id = 99, Name = "League A" };
        _context.Leagues.Add(league);
        _context.Teams.Add(new Team { Id = 99, LeagueId = 99, Name = "Team A" });
        _context.SaveChanges();

        // Act
        var result = _controller.GetTeamsByLeague(99) as JsonResult;

        // Assert
        Assert.NotNull(result);
        var teams = result.Value as IEnumerable<dynamic>;
        Assert.NotNull(teams);
        Assert.Single(teams);
    }

    [Fact]
    public void GetTeamsByLeague_InvalidLeagueId_ShouldReturnBadRequest()
    {
        // Act
        var result = _controller.GetTeamsByLeague(-1) as BadRequestObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }
}
