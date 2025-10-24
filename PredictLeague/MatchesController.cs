using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictLeague.Data;
using PredictLeague.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PredictLeague.Controllers
{
    public class MatchesController : Controller
    {
        private readonly PredictLeagueContext _context;

        public MatchesController(PredictLeagueContext context)
        {
            _context = context;
        }

        // GET: Matches
        public async Task<IActionResult> Index()
        {
            return View(await _context.Match.ToListAsync());
        }

        // GET: Matches/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var match = await _context.Match.FirstOrDefaultAsync(m => m.Id == id);
            if (match == null)
                return NotFound();

            return View(match);
        }

        // GET: Matches/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Matches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,HomeTeam,AwayTeam,StartTime,IsFinished,HomeScore,AwayScore")] Match match)
        {
            if (ModelState.IsValid)
            {
                _context.Add(match);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(match);
        }

        // GET: Matches/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var match = await _context.Match.FindAsync(id);
            if (match == null)
                return NotFound();

            return View(match);
        }

        // POST: Matches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,HomeTeam,AwayTeam,StartTime,IsFinished,HomeScore,AwayScore")] Match match)
        {
            if (id != match.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(match);
                    await _context.SaveChangesAsync();

                    // Изчисляване на точки
                    var predictions = await _context.Prediction.Where(p => p.MatchId == match.Id).ToListAsync();

                    foreach (var prediction in predictions)
                    {
                        prediction.Points = 0;

                        if (prediction.PredictedHomeScore == match.HomeScore &&
                            prediction.PredictedAwayScore == match.AwayScore)
                        {
                            prediction.Points = 3;
                        }
                        else if ((match.HomeScore > match.AwayScore && prediction.PredictedHomeScore > prediction.PredictedAwayScore) ||
                                 (match.HomeScore < match.AwayScore && prediction.PredictedHomeScore < prediction.PredictedAwayScore) ||
                                 (match.HomeScore == match.AwayScore && prediction.PredictedHomeScore == prediction.PredictedAwayScore))
                        {
                            prediction.Points = 1;
                        }

                        _context.Update(prediction);
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MatchExists(match.Id))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(match);
        }

        // GET: Matches/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var match = await _context.Match.FirstOrDefaultAsync(m => m.Id == id);
            if (match == null)
                return NotFound();

            return View(match);
        }

        // POST: Matches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var match = await _context.Match.FindAsync(id);
            if (match != null)
                _context.Match.Remove(match);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MatchExists(int id)
        {
            return _context.Match.Any(e => e.Id == id);
        }
    }
}
