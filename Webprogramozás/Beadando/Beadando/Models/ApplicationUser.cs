using Microsoft.AspNetCore.Identity;

namespace Beadando.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int? FavoriteLeagueId { get; set; } 
        public int? FavoriteTeamId { get; set; }
    }
}
