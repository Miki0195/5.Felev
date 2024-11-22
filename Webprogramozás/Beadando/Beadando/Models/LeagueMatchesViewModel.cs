using System;
using System.Collections.Generic;

namespace Beadando.Models
{
    public class LeagueMatchesViewModel
    {
        public string LeagueName { get; set; }
        public List<MatchViewModel> Matches { get; set; }
    }

}
