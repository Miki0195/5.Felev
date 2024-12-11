using System;
namespace Beadando.Models
{
	public class UserPreference
	{
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty; 
        public int FavoriteLeagueId { get; set; }
        public League FavoriteLeague { get; set; }

	}
}

