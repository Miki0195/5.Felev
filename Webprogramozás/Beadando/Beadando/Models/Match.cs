using System;
using System.Text.RegularExpressions;
using Beadando.Models;

namespace Beadando.Models
{ 
    public class Match
    {
        public int Id { get; set; }
        public int HomeTeamId { get; set; }
        public int AwayTeamId { get; set; }


        public Team HomeTeam { get; set; } // Navigation property
        public Team AwayTeam { get; set; } // Navigation property

        public string FinalScore { get; set; } = string.Empty;
        public string HalfTimeScore { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public string Report { get; set; } = string.Empty;

        public int LeagueId { get; set; }
        public League League { get; set; } // Foreign key to League

    }
}