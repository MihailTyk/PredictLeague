using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PredictLeague.Data;
using PredictLeague.Models;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PredictLeague.Controllers
{
    public class MatchesController : Controller
    {
        private readonly PredictLeagueContext _context;

        public MatchesController(PredictLeagueContext context)
        {
            _context = context;
        }

      
        public async Task<IActionResult> Index()
        {
            return View(await _context.Match.ToListAsync());
        }

       
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var match = await _context.Match.FirstOrDefaultAsync(m => m.Id == id);
            if (match == null)
                return NotFound();

            return View(match);
        }

      
        public IActionResult Create()
        {
            return View();
        }

       
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

      
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var match = await _context.Match.FindAsync(id);
            if (match == null)
                return NotFound();

            return View(match);
        }

        
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

       
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var match = await _context.Match.FirstOrDefaultAsync(m => m.Id == id);
            if (match == null)
                return NotFound();

            return View(match);
        }

       
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

       
        public async Task<IActionResult> Live()
        {
            string apiKey = "a1c5c63f7d7b71136b4512647b1da851";
            string url = "https://v3.football.api-sports.io/fixtures?league=39&season=2022";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-apisports-key", apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "⚠️ Грешка при зареждане на данните от API-то.";
                return View("Live", new List<FootballMatch>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<FootballApiResponse>(json);

            return View("Live", result.response);
        }
    }

   
    public class FootballApiResponse
    {
        public List<FootballMatch> response { get; set; }
    }

    public class FootballMatch
    {
        public Fixture fixture { get; set; }
        public Teams teams { get; set; }
        public Goals goals { get; set; }
    }

    public class Fixture
    {
        public DateTime date { get; set; }
        public Status status { get; set; }
    }

    public class Status
    {
        [JsonProperty("short")]
        public string short_ { get; set; }
    }

    public class Teams
    {
        public Team home { get; set; }
        public Team away { get; set; }
    }

    public class Team
    {
        public string name { get; set; }
        public string logo { get; set; }
    }

    public class Goals
    {
        public int? home { get; set; }
        public int? away { get; set; }
    }
}
