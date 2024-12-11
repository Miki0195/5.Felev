using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Beadando.Controllers;
using Beadando.Models;
using Beadando.Data;
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
    }

    [Fact]
    public void Create_Get_ShouldReturnViewResultWithTeamsAndLeagues()
    {
        // Arrange
        _context.Teams.AddRange(
            new Team { Id = 1, Name = "Team A" },
            new Team { Id = 2, Name = "Team B" }
        );
        _context.Leagues.AddRange(
            new League { Id = 1, Name = "League A" },
            new League { Id = 2, Name = "League B" }
        );
        _context.SaveChanges();

        // Act
        var result = _controller.Create() as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("~/Views/Match/Create.cshtml", result.ViewName);
    }

    [Fact]
    public void Create_Post_ValidModel_ShouldAddMatchAndReturnView()
    {
        // Arrange
        _context.Teams.AddRange(
            new Team { Id = 1, Name = "Team A" },
            new Team { Id = 2, Name = "Team B" }
        );
        _context.Leagues.Add(
            new League { Id = 1, Name = "League A" }
        );
        _context.SaveChanges();

        var match = new Match
        {
            HomeTeamId = 1,
            AwayTeamId = 2,
            LeagueId = 1,
            StartTime = System.DateTime.Now
        };

        // Act
        var result = _controller.Create(match) as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ViewData.ContainsKey("FormSubmitted"));
    }

    [Fact]
    public void DeleteMatch_ValidId_ShouldDeleteMatchAndRedirect()
    {
        // Arrange
        var match = new Match { Id = 1, HomeTeamId = 1, AwayTeamId = 2, LeagueId = 1 };
        _context.Matches.Add(match);
        _context.SaveChanges();

        // Act
        var result = _controller.DeleteMatch(1) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SearchMatches", result.ActionName);
        Assert.False(_context.Matches.Any(m => m.Id == 1));
    }

    [Fact]
    public void GetTeamsByLeague_ShouldReturnJsonResult()
    {
        // Arrange
        _context.Teams.Add(new Team { Id = 1, LeagueId = 1, Name = "Team A" });
        _context.SaveChanges();

        // Act
        var result = _controller.GetTeamsByLeague(1) as JsonResult;

        // Assert
        Assert.NotNull(result);
        Assert.Single((IEnumerable<dynamic>)result.Value);
    }
}



