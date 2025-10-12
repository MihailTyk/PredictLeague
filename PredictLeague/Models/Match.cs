using System;

namespace PredictLeague.Models
{
    public class Match
    {
        public int Id { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public DateTime StartTime { get; set; }
        public bool IsFinished { get; set; } = false;
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
    }
}
