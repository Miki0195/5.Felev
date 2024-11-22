using System;
using Beadando.Models;

namespace Beadando.Models
{
    public class TeamPerformanceViewModel
    {
        public string TeamName { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public List<MatchViewModel> Matches { get; set; }
    }
}

