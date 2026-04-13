using System;

namespace PredictLeague.Models
{
    public class Match
    {
        public int Id { get; set; }

        public string HomeTeam { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;

        public int? FixtureId { get; set; }
        public DateTime StartTime { get; set; }
        public bool IsFinished { get; set; } = false;

        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }

        public bool? HadPenalty { get; set; }

        // Нови полета за детайлна статистика (за изчисляване на точки)
        public int? ActualCorners { get; set; }
        public int? ActualOffsides { get; set; }
        public int? ActualYellowCards { get; set; }
        public int? ActualRedCards { get; set; }
        public string? ActualGoalscorers { get; set; } // Списък от имена, разделени със запетая
    }
}
