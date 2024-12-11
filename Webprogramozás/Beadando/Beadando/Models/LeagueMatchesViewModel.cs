using System;
using System.Collections.Generic;

namespace Beadando.Models
{
    public class LeagueMatchesViewModel
    {
        public string LeagueName { get; set; }
        public List<MatchViewModel> Matches { get; set; }
        public List<LeagueTableRow> LeagueTable { get; set; }
    }

    public class LeagueTableRow
    {
        public string TeamName { get; set; }
        public int MatchesPlayed { get; set; }
        public int Wins { get; set; }
        public int Ties { get; set; }
        public int Losses { get; set; }
        public int Points { get; set; }
    }

}
