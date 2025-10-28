using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PredictLeague.Data;
using PredictLeague.Models;

namespace PredictLeague.Controllers
{
    public class PredictionsController : Controller
    {
        private readonly PredictLeagueContext _context;

        public PredictionsController(PredictLeagueContext context)
        {
            _context = context;
        }

        // 🧾 Всички предсказания
        public async Task<IActionResult> Index()
        {
            var predictLeagueContext = _context.Prediction.Include(p => p.Match);
            return View(await predictLeagueContext.ToListAsync());
        }

        // 🧩 Детайли за конкретно Prediction
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var prediction = await _context.Prediction
                .Include(p => p.Match)
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

        // 💾 Създаване на Prediction (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,MatchId,UserName,PredictedHomeScore,PredictedAwayScore")] Prediction prediction)
        {
            // ✅ Премахваме Match от ModelState
            ModelState.Remove("Match");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "⚠️ Please fill all fields correctly!";
                return View(prediction);
            }

            try
            {
                prediction.CreatedAt = DateTime.Now;
                _context.Prediction.Add(prediction);
                await _context.SaveChangesAsync();

                // 🔍 Намираме мача
                var match = await _context.Match.FirstOrDefaultAsync(m => m.Id == prediction.MatchId);
                if (match != null && match.IsFinished)
                {
                    int points = 0;

                    // 🎯 Точен резултат
                    if (prediction.PredictedHomeScore == match.HomeScore &&
                        prediction.PredictedAwayScore == match.AwayScore)
                    {
                        points = 5;
                    }
                    // 🏆 Познат победител (но не точен резултат)
                    else if (
                        (match.HomeScore > match.AwayScore && prediction.PredictedHomeScore > prediction.PredictedAwayScore) ||
                        (match.HomeScore < match.AwayScore && prediction.PredictedHomeScore < prediction.PredictedAwayScore)
                    )
                    {
                        points = 3;
                    }
                    // ⚖️ Познато равенство (различен резултат)
                    else if (match.HomeScore == match.AwayScore && prediction.PredictedHomeScore == prediction.PredictedAwayScore)
                    {
                        points = 2;
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,MatchId,UserName,PredictedHomeScore,PredictedAwayScore,CreatedAt")] Prediction prediction)
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
