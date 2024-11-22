using System;

namespace Beadando.Models
{
    public class MatchViewModel
    {
        public int Id { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public string FinalScore { get; set; }
        public string HalfTimeScore { get; set; }
        public DateTime StartTime { get; set; }
        public string League { get; set; }
        public string Report { get; set; }
    }
}
