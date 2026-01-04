using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictLeague.Data;
using System.Linq;
using System.Threading.Tasks;

namespace PredictLeague.Controllers
{
    public class LeaderboardController : Controller
    {
        private readonly PredictLeagueContext _context;

        public LeaderboardController(PredictLeagueContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userStats = await _context.UserPlayers
                .Include(up => up.User)
                .GroupBy(up => up.User)
                .Select(g => new TeamLeaderboardEntry
                {
                    UserName = g.Key.UserName,
                    PlayerCount = g.Count(),
                    TotalRating = g.Sum(up => up.Rating),
                    BestPlayer = g.OrderByDescending(up => up.Rating).FirstOrDefault().PlayerName
                })
                .OrderByDescending(x => x.TotalRating)
                .ToListAsync();

            return View(userStats);
        }
    }

    public class TeamLeaderboardEntry
    {
        public string UserName { get; set; }
        public int PlayerCount { get; set; }
        public double TotalRating { get; set; }
        public string BestPlayer { get; set; }
    }
}
