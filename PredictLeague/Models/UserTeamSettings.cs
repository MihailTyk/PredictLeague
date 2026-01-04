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
    }
}
