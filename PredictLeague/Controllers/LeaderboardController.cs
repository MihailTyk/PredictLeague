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
            var userPlayers = await _context.UserPlayers.Include(up => up.User).ToListAsync();
            var teamSettings = await _context.UserTeamSettings.ToListAsync();

            var userStats = userPlayers
                .GroupBy(up => up.User)
                .Select(g => {
                    var settings = teamSettings.FirstOrDefault(s => s.UserId == g.Key.Id);
                    int totalCost = g.Sum(up => {
                        double r = up.Rating;
                        if (r >= 8.5) return 39;
                        if (r >= 8.0) return 32;
                        if (r >= 7.5) return 26;
                        if (r >= 7.0) return 19;
                        return 12;
                    });

                    return new TeamLeaderboardEntry
                    {
                        UserName = g.Key.UserName,
                        TeamName = settings?.TeamName ?? "Моят Отбор",
                        TeamBadgeUrl = settings?.TeamBadgeUrl ?? "https://cdn.pixabay.com/photo/2016/09/27/15/22/shield-1698650_1280.png",
                        PlayerCount = g.Count(),
                        TotalRating = g.Sum(up => up.Rating),
                        TotalCost = (int)totalCost,
                        BestPlayer = g.OrderByDescending(up => up.Rating).FirstOrDefault()?.PlayerName
                    };
                })
                .OrderByDescending(x => x.TotalCost)
                .ToList();

            return View(userStats);
        }
    }

    public class TeamLeaderboardEntry
    {
        public string UserName { get; set; }
        public string TeamName { get; set; }
        public string TeamBadgeUrl { get; set; }
        public int PlayerCount { get; set; }
        public double TotalRating { get; set; }
        public int TotalCost { get; set; }
        public string BestPlayer { get; set; }
    }
}
