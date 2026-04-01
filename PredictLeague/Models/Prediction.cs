using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace PredictLeague.Models
{
    public class Prediction
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Match")]
        public int MatchId { get; set; }
        public Match Match { get; set; }

        [Required]
        public string UserId { get; set; }     // <-- ВАЖНО!
        public IdentityUser User { get; set; } // <-- връзка към ASP.NET потребителя

        [Range(0, 20)]
        public int PredictedHomeScore { get; set; }

        [Range(0, 20)]
        public int PredictedAwayScore { get; set; }

        public int Points { get; set; } = 0;

        // Нови полета за допълнителни прогнози
        public string? PredictedWinner { get; set; } // "Home", "Away", "Draw"
        
        public bool BothTeamsToScore { get; set; } = false;

        // Още полета за детайлни прогнози
        public int? PredictedCorners { get; set; }
        public int? PredictedYellowCards { get; set; }
        public int? PredictedRedCards { get; set; }
        public int? PredictedOffsides { get; set; }
        
        public string? GoalScoringPrediction { get; set; } // "Both", "Home Only", "Away Only", "None"
        
        public string? AnytimeGoalscorer { get; set; } // Име на играч

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
