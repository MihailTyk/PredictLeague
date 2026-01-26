using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace PredictLeague.Models
{
    public class UserPlayer
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        public int PlayerApiId { get; set; }
        public string PlayerName { get; set; }
        public string Position { get; set; }
        public double Rating { get; set; }
        public string TeamName { get; set; } // Real life team
        
        public string? FieldPosition { get; set; } // e.g. "GK", "ST1", "CM2". Null if on bench.
        public bool IsStarter => !string.IsNullOrEmpty(FieldPosition);
    }
}
