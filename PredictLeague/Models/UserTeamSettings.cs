using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace PredictLeague.Models
{
    public class UserTeamSettings
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        public string Formation { get; set; } = "4-4-2"; // Default
        public int Points { get; set; } = 0; // Default Points for buying players
        public string TeamName { get; set; } = "Моят Отбор";
        public string TeamBadgeUrl { get; set; } = "https://cdn.pixabay.com/photo/2016/09/27/15/22/shield-1698650_1280.png"; // Default Badge
    }
}
