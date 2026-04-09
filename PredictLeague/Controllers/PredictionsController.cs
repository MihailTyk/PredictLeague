using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PredictLeague.Data;
using PredictLeague.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace PredictLeague.Controllers
{
    [Authorize]
    public class PredictionsController : Controller
    {
        private readonly PredictLeagueContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PredictionsController(PredictLeagueContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 🧾 Всички предсказания
        public async Task<IActionResult> Index()
        {
            var predictLeagueContext = _context.Prediction
                .Include(p => p.Match)
                .Include(p => p.User); // ако използваш навигация към User

            return View(await predictLeagueContext.ToListAsync());
        }

        // 🧩 Детайли за конкретно Prediction
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var prediction = await _context.Prediction
                .Include(p => p.Match)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (prediction == null)
                return NotFound();

            return View(prediction);
        }

        // ➕ Създаване на Prediction (GET)
        public IActionResult Create(int matchId)
        {
            var match = _context.Match.FirstOrDefault(m => m.Id == matchId);
            if (match == null)
                return NotFound();

            var prediction = new Prediction
            {
                MatchId = matchId,
                CreatedAt = DateTime.Now
            };

            return View(prediction);
        }

        // 💡 Създаване на мач локално от API-то преди предсказване
        [HttpPost]
        public async Task<IActionResult> CreateFromApi(int fixtureId, string homeTeam, string awayTeam, DateTime startTime, [Bind("PredictedHomeScore,PredictedAwayScore,PredictedCorners,PredictedYellowCards,PredictedRedCards,AnytimeGoalscorer")] Prediction prediction)
        {
            var match = await _context.Match.FirstOrDefaultAsync(m => m.FixtureId == fixtureId);

            if (match == null)
            {
                // Резервно търсене по имена и дата
                match = await _context.Match.FirstOrDefaultAsync(m => 
                    m.HomeTeam == homeTeam && 
                    m.AwayTeam == awayTeam && 
                    m.StartTime.Date == startTime.ToLocalTime().Date);
                
                if (match == null)
                {
                    match = new Match { 
                        FixtureId = fixtureId,
                        HomeTeam = homeTeam, 
                        AwayTeam = awayTeam, 
                        StartTime = startTime.ToLocalTime(), 
                        IsFinished = false 
                    };
                    _context.Match.Add(match);
                }
                else
                {
                    match.FixtureId = fixtureId; // Пренасяме ID-то ако го е нямало
                }
                await _context.SaveChangesAsync();
            }

            // 🛡️ Проверка за съществуваща прогноза за този потребител и мач
            var currentUserId = _userManager.GetUserId(User);
            var existing = await _context.Prediction
                .FirstOrDefaultAsync(p => p.MatchId == match.Id && p.UserId == currentUserId);

            if (existing != null)
            {
                // Актуализираме старата
                existing.PredictedHomeScore = prediction.PredictedHomeScore;
                existing.PredictedAwayScore = prediction.PredictedAwayScore;
                existing.PredictedCorners = prediction.PredictedCorners;
                existing.PredictedYellowCards = prediction.PredictedYellowCards;
                existing.PredictedRedCards = prediction.PredictedRedCards;
                existing.AnytimeGoalscorer = prediction.AnytimeGoalscorer;
                existing.CreatedAt = DateTime.Now;

                // Изчисляваме автоматично наново
                if (existing.PredictedHomeScore > existing.PredictedAwayScore) existing.PredictedWinner = "Home";
                else if (existing.PredictedHomeScore < existing.PredictedAwayScore) existing.PredictedWinner = "Away";
                else existing.PredictedWinner = "Draw";

                existing.BothTeamsToScore = (existing.PredictedHomeScore > 0 && existing.PredictedAwayScore > 0);
                
                _context.Update(existing);
            }
            else
            {
                // Записваме новата
                prediction.MatchId = match.Id;
                prediction.UserId = currentUserId;
                prediction.CreatedAt = DateTime.Now;

                if (prediction.PredictedHomeScore > prediction.PredictedAwayScore) prediction.PredictedWinner = "Home";
                else if (prediction.PredictedHomeScore < prediction.PredictedAwayScore) prediction.PredictedWinner = "Away";
                else prediction.PredictedWinner = "Draw";

                prediction.BothTeamsToScore = (prediction.PredictedHomeScore > 0 && prediction.PredictedAwayScore > 0);

                _context.Add(prediction);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "✅ Прогнозата е записана!";
            return RedirectToAction("Index", "MyPredictions");
        }

        // 💾 Създаване на Prediction (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,MatchId,PredictedHomeScore,PredictedAwayScore,PredictedWinner,BothTeamsToScore,PredictedCorners,PredictedYellowCards,PredictedRedCards,PredictedOffsides,GoalScoringPrediction,AnytimeGoalscorer")] Prediction prediction)
        {
            // махаме ModelState за Match
            ModelState.Remove("Match");
            ModelState.Remove("User");   // важно!
            ModelState.Remove("UserId"); // важно!

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "⚠️ Please fill all fields correctly!";
                return View(prediction);
            }

            try
            {
                // 👉 записваме кой user прави предикта
                prediction.UserId = _userManager.GetUserId(User);
                prediction.CreatedAt = DateTime.Now;

                // 🔄 Автоматично изчисляваме победителя и дали двата отбора вкарват
                if (prediction.PredictedHomeScore > prediction.PredictedAwayScore)
                    prediction.PredictedWinner = "Home";
                else if (prediction.PredictedHomeScore < prediction.PredictedAwayScore)
                    prediction.PredictedWinner = "Away";
                else
                    prediction.PredictedWinner = "Draw";

                prediction.BothTeamsToScore = (prediction.PredictedHomeScore > 0 && prediction.PredictedAwayScore > 0);
                
                // За GoalScoringPrediction също автоматизираме
                if (prediction.PredictedHomeScore > 0 && prediction.PredictedAwayScore > 0)
                    prediction.GoalScoringPrediction = "Both";
                else if (prediction.PredictedHomeScore > 0)
                    prediction.GoalScoringPrediction = "Home Only";
                else if (prediction.PredictedAwayScore > 0)
                    prediction.GoalScoringPrediction = "Away Only";
                else
                    prediction.GoalScoringPrediction = "None";

                _context.Prediction.Add(prediction);
                await _context.SaveChangesAsync();

                // 🔍 Намираме мача
                var match = await _context.Match.FirstOrDefaultAsync(m => m.Id == prediction.MatchId);
                if (match != null && match.IsFinished)
                {
                    int points = 0;

                    // 🎯 Точен резултат - 10 точки
                    if (prediction.PredictedHomeScore == match.HomeScore &&
                        prediction.PredictedAwayScore == match.AwayScore)
                    {
                        points = 10;
                    }
                    // ⚽ Познат само единия отбор головете - 3 точки
                    else if (prediction.PredictedHomeScore == match.HomeScore ||
                             prediction.PredictedAwayScore == match.AwayScore)
                    {
                        points = 3;
                    }
                    // ❌ Нищо не е познато - 0 точки
                    else
                    {
                        points = 0;
                    }

                    prediction.Points = points;
                    _context.Update(prediction);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "✅ Prediction saved successfully!";
                return RedirectToAction("Index", "Matches");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                TempData["Error"] = "❌ Something went wrong while saving your prediction!";
                return RedirectToAction("Index", "Matches");
            }
        }

        // ✏️ Редакция
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var prediction = await _context.Prediction.FindAsync(id);
            if (prediction == null)
                return NotFound();

            ViewData["MatchId"] = new SelectList(_context.Match, "Id", "Id", prediction.MatchId);
            return View(prediction);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,MatchId,PredictedHomeScore,PredictedAwayScore,CreatedAt,UserId")] Prediction prediction)
        {
            if (id != prediction.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(prediction);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PredictionExists(prediction.Id))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["MatchId"] = new SelectList(_context.Match, "Id", "Id", prediction.MatchId);
            return View(prediction);
        }

        // 🗑️ Изтриване
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var prediction = await _context.Prediction
                .Include(p => p.Match)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (prediction == null)
                return NotFound();

            return View(prediction);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var prediction = await _context.Prediction.FindAsync(id);
            if (prediction != null)
                _context.Prediction.Remove(prediction);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PredictionExists(int id)
        {
            return _context.Prediction.Any(e => e.Id == id);
        }
    }
}
