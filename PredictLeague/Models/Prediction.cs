using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public string UserName { get; set; } = string.Empty;

        [Range(0, 20)]
        public int PredictedHomeScore { get; set; }

        [Range(0, 20)]
        public int PredictedAwayScore { get; set; }

        public int Points { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
