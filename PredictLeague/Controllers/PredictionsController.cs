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

       
        public async Task<IActionResult> Index()
        {
            var predictLeagueContext = _context.Prediction.Include(p => p.Match);
            return View(await predictLeagueContext.ToListAsync());
        }

      
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

       
        public IActionResult Create(int matchId)
        {
            Console.WriteLine($"[DEBUG] Opening create for match ID: {matchId}");

            var match = _context.Match.FirstOrDefault(m => m.Id == matchId);
            if (match == null)
            {
                Console.WriteLine("[ERROR] Match not found!");
                return NotFound();
            }

            var prediction = new Prediction
            {
                MatchId = matchId,
                CreatedAt = DateTime.Now
            };

            return View(prediction);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Prediction prediction)
        {
            try
            {
               
                Console.WriteLine($"[DEBUG] Received prediction: MatchId={prediction.MatchId}, User={prediction.UserName}, Home={prediction.PredictedHomeScore}, Away={prediction.PredictedAwayScore}");

              
                if (prediction.MatchId == 0)
                {
                    Console.WriteLine("[ERROR] Missing MatchId");
                    TempData["Error"] = "Missing match ID!";
                    return RedirectToAction("Index", "Matches");
                }

               
                var match = await _context.Match.FirstOrDefaultAsync(m => m.Id == prediction.MatchId);
                if (match == null)
                {
                    Console.WriteLine("[ERROR] Match not found in DB!");
                    TempData["Error"] = "Match not found!";
                    return RedirectToAction("Index", "Matches");
                }

                if (ModelState.IsValid)
                {
                    prediction.CreatedAt = DateTime.Now;
                    _context.Prediction.Add(prediction);
                    await _context.SaveChangesAsync();

                    Console.WriteLine("[SUCCESS] Prediction saved successfully!");
                    TempData["Success"] = "Prediction saved successfully!";
                    return RedirectToAction("Index", "Matches");
                }

                Console.WriteLine("[ERROR] ModelState invalid!");
                return View(prediction);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXCEPTION] {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                TempData["Error"] = "An unexpected error occurred!";
                return RedirectToAction("Index", "Matches");
            }
        }

       
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
