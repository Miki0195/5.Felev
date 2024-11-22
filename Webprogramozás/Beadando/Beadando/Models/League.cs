using System;
using System.Text.RegularExpressions;

namespace Beadando.Models
{
    public class League
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Navigation property
        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }

}

