using System;
using Beadando.Models;
using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Beadando.Data
{
    public class SportsDbContext : IdentityDbContext<ApplicationUser>
    {
        public SportsDbContext(DbContextOptions<SportsDbContext> options) : base(options) { }

        public DbSet<League> Leagues { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Match - Team Relationships
            modelBuilder.Entity<Match>()
                .HasOne(m => m.HomeTeam)
                .WithMany(t => t.HomeMatches)
                .HasForeignKey(m => m.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<Match>()
                .HasOne(m => m.AwayTeam)
                .WithMany(t => t.AwayMatches)
                .HasForeignKey(m => m.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Match - League Relationship
            modelBuilder.Entity<Match>()
                .HasOne(m => m.League)
                .WithMany(l => l.Matches)
                .HasForeignKey(m => m.LeagueId);

            // Configure Team - League Relationship
            modelBuilder.Entity<Team>()
                .HasOne<League>()
                .WithMany()
                .HasForeignKey(t => t.LeagueId);

            // Seed Leagues
            modelBuilder.Entity<League>().HasData(
                new League { Id = 1, Name = "LaLiga" },
                new League { Id = 2, Name = "Premier League" }
            );

            // Seed Teams
            modelBuilder.Entity<Team>().HasData(
                // LaLiga
                new Team { Id = 1, Name = "Barcelona", LeagueId = 1 },
                new Team { Id = 2, Name = "Real Madrid", LeagueId = 1 },
                new Team { Id = 3, Name = "Atletico Madrid", LeagueId = 1 },
                new Team { Id = 4, Name = "Sevilla", LeagueId = 1 },
                new Team { Id = 5, Name = "Real Sociedad", LeagueId = 1 },
                new Team { Id = 6, Name = "Real Betis", LeagueId = 1 },
                new Team { Id = 7, Name = "Villarreal", LeagueId = 1 },
                new Team { Id = 8, Name = "Athletic Club", LeagueId = 1 },
                new Team { Id = 9, Name = "Valencia", LeagueId = 1 },
                new Team { Id = 10, Name = "Celta Vigo", LeagueId = 1 },
                new Team { Id = 11, Name = "Osasuna", LeagueId = 1 },
                new Team { Id = 12, Name = "Rayo Vallecano", LeagueId = 1 },
                new Team { Id = 13, Name = "Espanyol", LeagueId = 1 },
                new Team { Id = 14, Name = "Mallorca", LeagueId = 1 },
                new Team { Id = 15, Name = "Almeria", LeagueId = 1 },
                new Team { Id = 16, Name = "Girona", LeagueId = 1 },
                new Team { Id = 17, Name = "Cadiz", LeagueId = 1 },
                new Team { Id = 18, Name = "Getafe", LeagueId = 1 },
                new Team { Id = 19, Name = "Real Valladolid", LeagueId = 1 },
                new Team { Id = 20, Name = "Elche", LeagueId = 1 },

                // Premier League
                new Team { Id = 21, Name = "Arsenal", LeagueId = 2 },
                new Team { Id = 22, Name = "Manchester City", LeagueId = 2 },
                new Team { Id = 23, Name = "Manchester United", LeagueId = 2 },
                new Team { Id = 24, Name = "Liverpool", LeagueId = 2 },
                new Team { Id = 25, Name = "Chelsea", LeagueId = 2 },
                new Team { Id = 26, Name = "Tottenham Hotspur", LeagueId = 2 },
                new Team { Id = 27, Name = "Newcastle United", LeagueId = 2 },
                new Team { Id = 28, Name = "Brighton & Hove Albion", LeagueId = 2 },
                new Team { Id = 29, Name = "Brentford", LeagueId = 2 },
                new Team { Id = 30, Name = "Aston Villa", LeagueId = 2 },
                new Team { Id = 31, Name = "Fulham", LeagueId = 2 },
                new Team { Id = 32, Name = "Crystal Palace", LeagueId = 2 },
                new Team { Id = 33, Name = "Wolverhampton Wanderers", LeagueId = 2 },
                new Team { Id = 34, Name = "West Ham United", LeagueId = 2 },
                new Team { Id = 35, Name = "Leeds United", LeagueId = 2 },
                new Team { Id = 36, Name = "Leicester City", LeagueId = 2 },
                new Team { Id = 37, Name = "Everton", LeagueId = 2 },
                new Team { Id = 38, Name = "Southampton", LeagueId = 2 },
                new Team { Id = 39, Name = "Nottingham Forest", LeagueId = 2 },
                new Team { Id = 40, Name = "Bournemouth", LeagueId = 2 }
            );

            // Seed Matches
            modelBuilder.Entity<Match>().HasData(
                // Football Matches
                new Match
                {
                    Id = 1,
                    HomeTeamId = 1, // Barcelona
                    AwayTeamId = 2, // Real Madrid
                    FinalScore = "3:2",
                    HalfTimeScore = "2:1",
                    StartTime = new DateTime(2024, 12, 4, 18, 0, 0), 
                    Report = "Barcelona wins a thrilling El Clasico!",
                    LeagueId = 1 // La Liga
                },
                new Match
                {
                    Id = 2,
                    HomeTeamId = 3, // Atletico Madrid
                    AwayTeamId = 4, // Sevilla
                    FinalScore = "1:1",
                    HalfTimeScore = "0:1",
                    StartTime = new DateTime(2024, 12, 5, 18, 0, 0),
                    Report = "Atletico Madrid equalizes late in the game.",
                    LeagueId = 1 // La Liga
                },
                new Match
                {
                    Id = 3,
                    HomeTeamId = 5, // Real Sociedad
                    AwayTeamId = 6, // Real Betis
                    FinalScore = "0:2",
                    HalfTimeScore = "0:1",
                    StartTime = new DateTime(2024, 12, 6, 18, 0, 0),
                    Report = "Real Betis dominates the game.",
                    LeagueId = 1 // La Liga
                },
                new Match
                {
                    Id = 4,
                    HomeTeamId = 7, // Villarreal
                    AwayTeamId = 8, // Athletic Club
                    FinalScore = "2:2",
                    HalfTimeScore = "1:0",
                    StartTime = new DateTime(2024, 12, 7, 18, 0, 0),
                    Report = "A balanced game ends in a draw.",
                    LeagueId = 1 // La Liga
                },
                new Match
                {
                    Id = 5,
                    HomeTeamId = 9, // Valencia
                    AwayTeamId = 10, // Celta Vigo
                    FinalScore = "1:3",
                    HalfTimeScore = "0:2",
                    StartTime = new DateTime(2024, 12, 8, 18, 0, 0),
                    Report = "Celta Vigo shocks Valencia with a strong performance.",
                    LeagueId = 1 // La Liga
                },
                new Match
                {
                    Id = 6,
                    HomeTeamId = 21, // Arsenal
                    AwayTeamId = 22, // Manchester City
                    FinalScore = "2:0",
                    HalfTimeScore = "1:0",
                    StartTime = new DateTime(2024, 12, 9, 18, 0, 0),
                    Report = "Arsenal outclasses Manchester City.",
                    LeagueId = 2 // Premier League
                },
                new Match
                {
                    Id = 7,
                    HomeTeamId = 23, // Manchester United
                    AwayTeamId = 24, // Liverpool
                    FinalScore = "1:1",
                    HalfTimeScore = "0:0",
                    StartTime = new DateTime(2024, 12, 10, 18, 0, 0),
                    Report = "A tense derby ends in a draw.",
                    LeagueId = 2 // Premier League
                },
                new Match
                {
                    Id = 8,
                    HomeTeamId = 25, // Chelsea
                    AwayTeamId = 26, // Tottenham Hotspur
                    FinalScore = "3:1",
                    HalfTimeScore = "2:0",
                    StartTime = new DateTime(2024, 12, 11, 18, 0, 0),
                    Report = "Chelsea secures a convincing win over Spurs.",
                    LeagueId = 2 // Premier League
                },
                new Match
                {
                    Id = 9,
                    HomeTeamId = 27, // Newcastle United
                    AwayTeamId = 28, // Brighton & Hove Albion
                    FinalScore = "0:1",
                    HalfTimeScore = "0:0",
                    StartTime = new DateTime(2024, 12, 12, 18, 0, 0),
                    Report = "Brighton edges out Newcastle with a late goal.",
                    LeagueId = 2 // Premier League
                },
                new Match
                {
                    Id = 10,
                    HomeTeamId = 29, // Brentford
                    AwayTeamId = 30, // Aston Villa
                    FinalScore = "2:2",
                    HalfTimeScore = "1:2",
                    StartTime = new DateTime(2024, 12, 13, 18, 0, 0),
                    Report = "Aston Villa and Brentford share points in a thrilling match.",
                    LeagueId = 2 // Premier League
                },
                new Match
                {
                    Id = 11,
                    HomeTeamId = 1, // Barcelona
                    AwayTeamId = 3, // Atletico Madrid
                    FinalScore = "1:1",
                    HalfTimeScore = "1:0",
                    StartTime = new DateTime(2024, 12, 14, 18, 0, 0),
                    Report = "Barcelona and Atletico Madrid settle for a draw.",
                    LeagueId = 1 // La Liga
                },
                new Match
                {
                    Id = 12,
                    HomeTeamId = 22, // Manchester City
                    AwayTeamId = 23, // Manchester United
                    FinalScore = "2:2",
                    HalfTimeScore = "1:2",
                    StartTime = new DateTime(2024, 12, 15, 18, 0, 0),
                    Report = "The Manchester derby ends in a dramatic draw.",
                    LeagueId = 2 // Premier League
                }


            );

        }
    }
}

