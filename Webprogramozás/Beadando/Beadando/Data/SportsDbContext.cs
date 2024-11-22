using System;
using Beadando.Models;
using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Beadando.Data
{
    public class SportsDbContext : IdentityDbContext<IdentityUser>
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
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

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

            // Seed Leagues
            modelBuilder.Entity<League>().HasData(
                new League { Id = 1, Name = "Football" },
                new League { Id = 2, Name = "Basketball" }
            );

            // Seed Teams
            modelBuilder.Entity<Team>().HasData(
                new Team { Id = 1, Name = "Manchester United" },
                new Team { Id = 2, Name = "Real Madrid" },
                new Team { Id = 3, Name = "Liverpool" },
                new Team { Id = 4, Name = "Barcelona" },
                new Team { Id = 5, Name = "Los Angeles Lakers" },
                new Team { Id = 6, Name = "Golden State Warriors" },
                new Team { Id = 7, Name = "Chicago Bulls" },
                new Team { Id = 8, Name = "Miami Heat" }
            );

            // Seed Matches
            modelBuilder.Entity<Match>().HasData(
                // Football Matches
                new Match
                {
                    Id = 1,
                    HomeTeamId = 1, // Manchester United
                    AwayTeamId = 2, // Real Madrid
                    FinalScore = "2:1",
                    HalfTimeScore = "1:1",
                    StartTime = new DateTime(2023, 11, 1, 18, 0, 0), // Static date
                    Report = "Manchester United secured a victory in the final minutes!",
                    LeagueId = 1 // Football
                },
                new Match
                {
                    Id = 2,
                    HomeTeamId = 3, // Liverpool
                    AwayTeamId = 4, // Barcelona
                    FinalScore = "3:2",
                    HalfTimeScore = "2:1",
                    StartTime = new DateTime(2023, 11, 3, 20, 30, 0), // Static date
                    Report = "Liverpool edged out Barcelona in a thrilling game.",
                    LeagueId = 1 // Football
                },

                // Basketball Matches
                new Match
                {
                    Id = 3,
                    HomeTeamId = 5, // Los Angeles Lakers
                    AwayTeamId = 6, // Golden State Warriors
                    FinalScore = "112:108",
                    HalfTimeScore = "55:50",
                    StartTime = new DateTime(2023, 11, 5, 19, 0, 0), // Static date
                    Report = "LeBron James led the Lakers to a narrow victory over the Warriors.",
                    LeagueId = 2 // Basketball
                },
                new Match
                {
                    Id = 4,
                    HomeTeamId = 7, // Chicago Bulls
                    AwayTeamId = 8, // Miami Heat
                    FinalScore = "98:95",
                    HalfTimeScore = "45:50",
                    StartTime = new DateTime(2023, 11, 6, 21, 0, 0), // Static date
                    Report = "The Bulls triumphed in a tightly contested match.",
                    LeagueId = 2 // Basketball
                },
                new Match
                {
                    Id = 5,
                    HomeTeamId = 4, 
                    AwayTeamId = 3, 
                    FinalScore = "4:0",
                    HalfTimeScore = "1:0",
                    StartTime = new DateTime(2024, 11, 21, 19, 0, 0),
                    Report = "Epic victory for Barcelona",
                    LeagueId = 1
                }
                
            );
        }
    }
}

