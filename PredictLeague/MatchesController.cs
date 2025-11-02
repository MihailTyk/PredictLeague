using Microsoft.AspNetCore.Authorization;
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

        // 🏟️ Всички мачове — достъпно за всички
        public async Task<IActionResult> Index()
        {
            return View(await _context.Match.ToListAsync());
        }

        // 🔍 Детайли за мач — достъпно за всички
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var match = await _context.Match.FirstOrDefaultAsync(m => m.Id == id);
            if (match == null)
                return NotFound();

            return View(match);
        }

        // ➕ Създаване на нов мач — само за Admin
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // 💾 Създаване (POST) — само за Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
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

        // ✏️ Редакция на мач — само за Admin
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var match = await _context.Match.FindAsync(id);
            if (match == null)
                return NotFound();

            return View(match);
        }

        // 💾 Записване на редакцията — само за Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
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

                    // 🏆 Изчисляваме точките след приключване на мача
                    var predictions = await _context.Prediction.Where(p => p.MatchId == match.Id).ToListAsync();

                    foreach (var prediction in predictions)
                    {
                        prediction.Points = 0;

                        if (prediction.PredictedHomeScore == match.HomeScore &&
                            prediction.PredictedAwayScore == match.AwayScore)
                        {
                            prediction.Points = 3; // Точно познат резултат
                        }
                        else if (
                            (match.HomeScore > match.AwayScore && prediction.PredictedHomeScore > prediction.PredictedAwayScore) ||
                            (match.HomeScore < match.AwayScore && prediction.PredictedHomeScore < prediction.PredictedAwayScore) ||
                            (match.HomeScore == match.AwayScore && prediction.PredictedHomeScore == prediction.PredictedAwayScore)
                        )
                        {
                            prediction.Points = 1; // Познат изход (победа/загуба/равен)
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

        // ❌ Изтриване на мач — само за Admin
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var match = await _context.Match.FirstOrDefaultAsync(m => m.Id == id);
            if (match == null)
                return NotFound();

            return View(match);
        }

        // 💣 Потвърждение на изтриването — само за Admin
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
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

        // ⚽ Универсален метод за зареждане на мачове по лига
        private async Task<IActionResult> LoadLeagueMatches(string leagueName, int leagueId)
        {
            string apiKey = "a1c5c63f7d7b71136b4512647b1da851";
            int currentSeason = DateTime.Now.Month >= 8 ? DateTime.Now.Year : DateTime.Now.Year - 1;

            string url = $"https://v3.football.api-sports.io/fixtures?league={leagueId}&season={currentSeason}";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-apisports-key", apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = $"⚠️ Неуспешно зареждане на {leagueName} от API.";
                return View("League", new List<FootballMatch>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<FootballApiResponse>(json);

            if (result == null || result.response == null || !result.response.Any())
            {
                ViewBag.Error = $"❌ Няма налични мачове за {leagueName} ({currentSeason}).";
                return View("League", new List<FootballMatch>());
            }

            ViewBag.LeagueName = leagueName;
            ViewBag.Season = currentSeason;
            return View("League", result.response);
        }

        // 🏴 Premier League
        public async Task<IActionResult> PremierLeague()
        {
            return await LoadLeagueMatches("Premier League", 39);
        }

        // 🇪🇸 La Liga
        public async Task<IActionResult> LaLiga()
        {
            return await LoadLeagueMatches("La Liga", 140);
        }

        // 🇮🇹 Serie A
        public async Task<IActionResult> SerieA()
        {
            return await LoadLeagueMatches("Serie A", 135);
        }

        // 🇩🇪 Bundesliga
        public async Task<IActionResult> Bundesliga()
        {
            return await LoadLeagueMatches("Bundesliga", 78);
        }

        // 🏆 Champions League
        public async Task<IActionResult> ChampionsLeague()
        {
            return await LoadLeagueMatches("Champions League", 2);
        }
    }

    // 🧩 Модели за Football API
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
