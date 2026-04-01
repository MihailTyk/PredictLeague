using System.ComponentModel.DataAnnotations;

namespace PredictLeague.Models
{
    public class AdminUserViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public int TotalPredictions { get; set; }
        public int TotalPoints { get; set; }
    }
}
