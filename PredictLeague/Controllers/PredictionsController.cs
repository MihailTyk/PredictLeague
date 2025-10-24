using System;
using System.Collections.Generic;
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

        // ✅ GET: Predictions
        public async Task<IActionResult> Index()
        {
            var predictLeagueContext = _context.Prediction.Include(p => p.Match);
            return View(await predictLeagueContext.ToListAsync());
        }

        // ✅ GET: Predictions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prediction = await _context.Prediction
                .Include(p => p.Match)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (prediction == null)
            {
                return NotFound();
            }

            return View(prediction);
        }

        // ✅ GET: Predictions/Create?matchId=1
        public IActionResult Create(int matchId)
        {
            var match = _context.Match.FirstOrDefault(m => m.Id == matchId);
            if (match == null)
            {
                return NotFound();
            }

            var prediction = new Prediction
            {
                MatchId = matchId,
                CreatedAt = DateTime.Now
            };

            return View(prediction);
        }

        // ✅ POST: Predictions/Create (Save button)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Prediction prediction)
        {
            if (prediction.MatchId == 0)
            {
                return BadRequest("Missing match ID.");
            }

            var match = await _context.Match.FirstOrDefaultAsync(m => m.Id == prediction.MatchId);
            if (match == null)
            {
                return NotFound("Match not found.");
            }

            if (ModelState.IsValid)
            {
                prediction.CreatedAt = DateTime.Now;
                _context.Add(prediction);
                await _context.SaveChangesAsync();

                // ✅ След успешен запис връщаме потребителя към Matches
                return RedirectToAction("Index", "Matches");
            }

            return View(prediction);
        }

        // ✅ GET: Predictions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prediction = await _context.Prediction.FindAsync(id);
            if (prediction == null)
            {
                return NotFound();
            }
            ViewData["MatchId"] = new SelectList(_context.Match, "Id", "Id", prediction.MatchId);
            return View(prediction);
        }

        // ✅ POST: Predictions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,MatchId,UserName,PredictedHomeScore,PredictedAwayScore,CreatedAt")] Prediction prediction)
        {
            if (id != prediction.Id)
            {
                return NotFound();
            }

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
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MatchId"] = new SelectList(_context.Match, "Id", "Id", prediction.MatchId);
            return View(prediction);
        }

        // ✅ GET: Predictions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prediction = await _context.Prediction
                .Include(p => p.Match)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (prediction == null)
            {
                return NotFound();
            }

            return View(prediction);
        }

        // ✅ POST: Predictions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var prediction = await _context.Prediction.FindAsync(id);
            if (prediction != null)
            {
                _context.Prediction.Remove(prediction);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PredictionExists(int id)
        {
            return _context.Prediction.Any(e => e.Id == id);
        }
    }
}
