using System;

namespace PredictLeague.Models
{
    public class Match
    {
        public int Id { get; set; }

        public string HomeTeam { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
        public bool IsFinished { get; set; } = false;

        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
    }
}
