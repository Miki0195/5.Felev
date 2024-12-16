using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Beadando.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Beadando.Models
{ 
    public class Match
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Home Team is required.")]
        public int? HomeTeamId { get; set; }

        [Required(ErrorMessage = "Away Team is required.")]
        public int? AwayTeamId { get; set; }

        [ValidateNever] 
        public Team? HomeTeam { get; set; }

        [ValidateNever] 
        public Team? AwayTeam { get; set; }

        [RegularExpression(@"^\d+:\d+$", ErrorMessage = "Final Score must be in the format '0:0'.")]
        public string FinalScore { get; set; } = string.Empty;

        [RegularExpression(@"^\d+:\d+$", ErrorMessage = "Half-Time Score must be in the format '0:0'.")]
        public string HalfTimeScore { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start Time is required.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Report is required.")]
        public string Report { get; set; } = string.Empty;

        [Required(ErrorMessage = "League is required.")]
        public int? LeagueId { get; set; }

        [ValidateNever] 
        public League? League { get; set; }

    }
}