using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PredictLeague.Models;

namespace PredictLeague.Data
{
    public class PredictLeagueContext : IdentityDbContext
    {
        public PredictLeagueContext(DbContextOptions<PredictLeagueContext> options)
            : base(options)
        {
        }

        public DbSet<Match> Match { get; set; }
        public DbSet<Prediction> Prediction { get; set; }
        public DbSet<UserPlayer> UserPlayers { get; set; }
        public DbSet<UserTeamSettings> UserTeamSettings { get; set; }
        public DbSet<WeeklyRewardHistory> WeeklyRewardHistory { get; set; }
    }
}
