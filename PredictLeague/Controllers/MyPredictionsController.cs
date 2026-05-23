using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictLeague.Data;

namespace PredictLeague.Controllers
{
    [Authorize] 
    public class MyPredictionsController : Controller
    {
        private readonly PredictLeagueContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MyPredictionsController> _logger;

        public MyPredictionsController(PredictLeagueContext context, UserManager<IdentityUser> userManager, IConfiguration configuration, ILogger<MyPredictionsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return LocalRedirect("/Identity/Account/Login");

            var predictions = await _context.Prediction
                .Include(p => p.Match)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // --- АВТОМАТИЧНА СИНХРОНИЗАЦИЯ НА БЮДЖЕТА ---
            var teamSettings = await _context.UserTeamSettings.FirstOrDefaultAsync(s => s.UserId == userId);
            var totalEarned = predictions.Sum(p => p.Points);
            
            if (teamSettings == null && totalEarned > 0)
            {
                teamSettings = new Models.UserTeamSettings { UserId = userId, Points = totalEarned, Formation = "4-4-2" };
                _context.UserTeamSettings.Add(teamSettings);
                await _context.SaveChangesAsync();
            }
            // --------------------------------------------

            return View(predictions);
        }

        [HttpPost]
        public async Task<IActionResult> SyncBudget()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return LocalRedirect("/Identity/Account/Login");
            var totalPointsEarned = await _context.Prediction
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.Points);

            var teamSettings = await _context.UserTeamSettings.FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (teamSettings != null)
            {
                // Понеже бюджетът се променя при покупка, не можем просто да го сетнем.
                // Но можем да се уверим, че е поне колкото спечелените точки, ако потребителят няма играчи.
                var playersCount = await _context.UserPlayers.CountAsync(up => up.UserId == userId);
                
                if (playersCount == 0 && teamSettings.Points < totalPointsEarned)
                {
                    teamSettings.Points = totalPointsEarned;
                    _context.Update(teamSettings);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Бюджетът беше синхронизиран! Вече имате {totalPointsEarned} точки.";
                }
                else if (playersCount > 0)
                {
                    // Ако има играчи, логиката е по-сложна, но можем поне да добавим пропуснатите точки.
                    // За момента приемаме, че ако са се появили нови точки в прогнозите, те трябва да са в бюджета.
                    // Връщаме съобщение.
                    TempData["Info"] = "Точките ви се изчисляват автоматично при завършване на мачовете.";
                }
            }
            else
            {
                // Ако няма настройки, създаваме ги с правилните точки
                teamSettings = new Models.UserTeamSettings 
                { 
                    UserId = userId, 
                    Points = totalPointsEarned, 
                    Formation = "4-4-2" 
                };
                _context.UserTeamSettings.Add(teamSettings);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Бюджетът беше инициализиран успешно!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
