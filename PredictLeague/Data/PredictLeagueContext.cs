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

        public DbSet<Match> Match { get; set; } = default!;
        public DbSet<Prediction> Prediction { get; set; } = default!;
    }
}
