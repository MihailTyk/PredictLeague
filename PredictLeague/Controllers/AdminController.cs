using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictLeague.Data;
using PredictLeague.Models;

namespace PredictLeague.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly PredictLeagueContext _context;

        public AdminController(PredictLeagueContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            var predictions = await _context.Prediction.ToListAsync();

            // Изчисляваме кой колко прогнози има и колко са му total точките от тях
            var viewModel = users.Select(u => new AdminUserViewModel
            {
                UserId = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                TotalPredictions = predictions.Count(p => p.UserId == u.Id),
                TotalPoints = predictions.Where(p => p.UserId == u.Id).Sum(p => p.Points)
            })
            .OrderByDescending(x => x.TotalPoints)
            .ToList();

            // Изчисляваме ТОП 3 ЗА СЕДМИЦАТА (последни 7 дни)
            var lastWeek = DateTime.Now.AddDays(-7);
            var weeklyWinners = predictions
                .Where(p => p.CreatedAt >= lastWeek)
                .GroupBy(p => p.UserId)
                .Select(g => new WeeklyWinnerViewModel
                {
                    UserId = g.Key,
                    UserName = users.FirstOrDefault(u => u.Id == g.Key)?.UserName ?? "Unknown",
                    PointsThisWeek = g.Sum(p => p.Points)
                })
                .OrderByDescending(x => x.PointsThisWeek)
                .Take(3)
                .ToList();

            ViewBag.WeeklyWinners = weeklyWinners;

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DistributeWeeklyRewards()
        {
            var lastWeek = DateTime.Now.AddDays(-7);
            var users = await _context.Users.ToListAsync(); // Нужни са ни имената
            var top3 = await _context.Prediction
                .Where(p => p.CreatedAt >= lastWeek)
                .GroupBy(p => p.UserId)
                .Select(g => new { UserId = g.Key, Points = g.Sum(p => p.Points) })
                .OrderByDescending(x => x.Points)
                .Take(3)
                .ToListAsync();

            if (!top3.Any())
            {
                TempData["Info"] = "Няма активни играчи за последната седмица.";
                return RedirectToAction(nameof(Index));
            }

            int[] prizes = { 200, 150, 100 };
            int distributedCount = 0;

            for (int i = 0; i < top3.Count; i++)
            {
                var winner = top3[i];
                var settings = await _context.UserTeamSettings.FirstOrDefaultAsync(s => s.UserId == winner.UserId);
                if (settings != null)
                {
                    settings.Points += prizes[i];
                    _context.Update(settings);
                    distributedCount++;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Успешно раздадени награди на {distributedCount} играчи!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RecalculateAllPoints()
        {
            var predictions = await _context.Prediction.Include(p => p.Match).ToListAsync();
            var userSettings = await _context.UserTeamSettings.ToListAsync();

            // 1. Нулираме точките в настройките на потребителите
            foreach (var setting in userSettings)
            {
                setting.Points = 0;
            }

            // 2. Преизчисляваме всяка прогноза
            foreach (var p in predictions)
            {
                if (!p.Match.IsFinished) continue;

                int newPoints = 0;
                if (p.PredictedHomeScore == p.Match.HomeScore && p.PredictedAwayScore == p.Match.AwayScore)
                {
                    newPoints = 10;
                }
                else
                {
                    if (p.PredictedHomeScore == p.Match.HomeScore) newPoints += 3;
                    if (p.PredictedAwayScore == p.Match.AwayScore) newPoints += 3;

                    if (newPoints == 0)
                    {
                        bool outcomeMatches = (p.Match.HomeScore > p.Match.AwayScore && p.PredictedHomeScore > p.PredictedAwayScore) ||
                                             (p.Match.HomeScore < p.Match.AwayScore && p.PredictedHomeScore < p.PredictedAwayScore) ||
                                             (p.Match.HomeScore == p.Match.AwayScore && p.PredictedHomeScore == p.PredictedAwayScore);
                        if (outcomeMatches) newPoints = 1;
                    }
                }

                // 🎯 БОНУС ТОЧКИ (Детайли)
                
                // 1. Дузпа (+3т) - САМО ако е заложил "Да" и мачът завърши с дузпа
                if (p.Match.HadPenalty == true && p.PredictedPenalty == true)
                {
                    newPoints += 3;
                }

                // 2. Голмайстор (+5т)
                if (!string.IsNullOrEmpty(p.AnytimeGoalscorer) && !string.IsNullOrEmpty(p.Match.ActualGoalscorers))
                {
                    var scorersList = p.Match.ActualGoalscorers.Split(", ", StringSplitOptions.RemoveEmptyEntries);
                    // Проверка дали избраният играч е в списъка с голмайстори
                    if (scorersList.Any(s => s.Contains(p.AnytimeGoalscorer, StringComparison.OrdinalIgnoreCase) || 
                                            p.AnytimeGoalscorer.Contains(s, StringComparison.OrdinalIgnoreCase)))
                    {
                        newPoints += 5;
                    }
                }

                // 3. Корнери (+3т)
                if (p.PredictedCorners.HasValue && p.Match.ActualCorners.HasValue)
                {
                    if (p.PredictedCorners.Value == p.Match.ActualCorners.Value) newPoints += 3;
                }

                // 4. Засади (+2т)
                if (p.PredictedOffsides.HasValue && p.Match.ActualOffsides.HasValue)
                {
                    if (p.PredictedOffsides.Value == p.Match.ActualOffsides.Value) newPoints += 2;
                }

                // 5. Жълти картони (+2т)
                if (p.PredictedYellowCards.HasValue && p.Match.ActualYellowCards.HasValue)
                {
                    if (p.PredictedYellowCards.Value == p.Match.ActualYellowCards.Value) newPoints += 2;
                }

                // 6. Червени картони (+2т)
                if (p.PredictedRedCards.HasValue && p.Match.ActualRedCards.HasValue)
                {
                    if (p.PredictedRedCards.Value == p.Match.ActualRedCards.Value) newPoints += 2;
                }

                p.Points = newPoints;
                
                // Добавяме към бюджета на потребителя
                var setting = userSettings.FirstOrDefault(s => s.UserId == p.UserId);
                if (setting != null)
                {
                    setting.Points += newPoints;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Всички точки бяха преизчислени по новите правила!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> UserPredictions(string id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) 
            {
                return NotFound();
            }

            var userPredictions = await _context.Prediction
                .Include(p => p.Match)
                .Where(p => p.UserId == id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.UserName = user.UserName;
            ViewBag.TotalPoints = userPredictions.Sum(p => p.Points);

            return View(userPredictions);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Охраняваме главния админ
            if (user.Email != null && user.Email.Equals("admin@predictleague.com", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Не можете да изтриете главния администратор!";
                return RedirectToAction(nameof(Index));
            }

            // Изтриваме свързаните записи, за да избегнем грешки с Foreign Keys
            var predictions = _context.Prediction.Where(p => p.UserId == id);
            _context.Prediction.RemoveRange(predictions);

            var players = _context.UserPlayers.Where(p => p.UserId == id);
            _context.UserPlayers.RemoveRange(players);

            var settings = _context.UserTeamSettings.Where(s => s.UserId == id);
            _context.UserTeamSettings.RemoveRange(settings);

            // Изтриваме самия потребител
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Потребителят {user.UserName} беше успешно изтрит.";
            return RedirectToAction(nameof(Index));
        }
    }

    public class AdminUserViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public int TotalPredictions { get; set; }
        public int TotalPoints { get; set; }
    }

    public class WeeklyWinnerViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int PointsThisWeek { get; set; }
    }
}
