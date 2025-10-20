using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PredictLeague.Models;

namespace PredictLeague.Data
{
    public class PredictLeagueContext : DbContext
    {
        public PredictLeagueContext (DbContextOptions<PredictLeagueContext> options)
            : base(options)
        {
        }

        public DbSet<PredictLeague.Models.Match> Match { get; set; } = default!;
        public DbSet<Prediction> Prediction { get; set; }

    }
}
