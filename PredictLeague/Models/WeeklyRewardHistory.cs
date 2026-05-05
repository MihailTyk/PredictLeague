using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace PredictLeague.Models
{
    public class WeeklyRewardHistory
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        public int Position { get; set; }       // 1, 2 или 3
        public int PointsAwarded { get; set; }  // 200, 150 или 100

        public DateTime DistributedAt { get; set; } = DateTime.Now;
    }
}
