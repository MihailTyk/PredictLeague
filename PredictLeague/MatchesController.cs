using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<MatchesController> _logger;

        public MatchesController(PredictLeagueContext context, ILogger<MatchesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 🏟️ Всички мачове — достъпно за всички
        public IActionResult Index()
        {
            return View();
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
            try
            {
                var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                string apiKey = configuration["ApiKeys:ApiSports"] ?? "";
                
                // Безплатният план на API-Sports.io (ако има промяна) или нормален ключ
                // Използваме най-новите достъпни сезони спрямо 2026-та година
                int[] seasonsToTry = { 2026, 2025, 2024, 2023, 2022, 2021 };

                string url = "";
                var response = (System.Net.Http.HttpResponseMessage?)null;
                var json = "";
                FootballApiResponse? result = null;
                
                foreach (var season in seasonsToTry)
                {
                    url = $"https://v3.football.api-sports.io/fixtures?league={leagueId}&season={season}";

                    _logger.LogInformation($"Trying {leagueName} matches for season {season}. URL: {url.Replace(apiKey, "***")}");

                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("x-apisports-key", apiKey);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.Timeout = TimeSpan.FromSeconds(30);

                    response = await client.GetAsync(url);

                    _logger.LogInformation($"API Response Status for season {season}: {response.StatusCode}");

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"API request failed for season {season}. Status: {response.StatusCode}, Response: {errorContent}");
                        
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            ViewBag.Error = $"❌ API ключът е невалиден или изтекъл. Провери API ключа за {leagueName}.";
                            return View("League", new List<FootballMatch>());
                        }
                        
                        // Опитай следващия сезон
                        continue;
                    }

                    json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Response received for season {season}. Length: {json.Length} characters");
                    
                    // Логваме първите 500 символа за дебъгване
                    if (json.Length > 0)
                    {
                        _logger.LogInformation($"Response preview (first 500 chars): {json.Substring(0, Math.Min(500, json.Length))}");
                    }
                    
                    result = JsonConvert.DeserializeObject<FootballApiResponse>(json);

                    // Проверяваме дали има errors в response-а
                    if (result != null && result.errors != null)
                    {
                        string errorMessage = "";
                        
                        // Обработваме errors като обект или масив
                        if (result.errors is Newtonsoft.Json.Linq.JObject errorsObj)
                        {
                            var errorValues = errorsObj.Properties()
                                .Select(p => p.Value?.ToString() ?? "")
                                .Where(v => !string.IsNullOrEmpty(v))
                                .ToList();
                            errorMessage = string.Join(", ", errorValues);
                        }
                        else if (result.errors is Newtonsoft.Json.Linq.JArray errorsArray)
                        {
                            var errorValues = errorsArray
                                .Select(item => item?.ToString() ?? "")
                                .Where(v => !string.IsNullOrEmpty(v))
                                .ToList();
                            errorMessage = string.Join(", ", errorValues);
                        }
                        else
                        {
                            errorMessage = result.errors.ToString();
                        }
                        
                        if (!string.IsNullOrEmpty(errorMessage))
                        {
                            _logger.LogWarning($"API returned errors for season {season}: {errorMessage}");
                            
                            // Ако грешката е за плана, показваме по-ясно съобщение
                            if (errorMessage.Contains("Free plans") || errorMessage.Contains("plan"))
                            {
                                _logger.LogInformation($"Skipping season {season} due to plan restrictions");
                                continue; // Опитай следващия сезон
                            }
                        }
                    }

                    if (result != null && result.response != null && result.response.Any())
                    {
                        var futureMatches = result.response
                            .Where(m => m.fixture.date >= DateTime.UtcNow)
                            .OrderBy(m => m.fixture.date)
                            .ToList();

                        _logger.LogInformation($"Successfully loaded {result.response.Count} matches, showed {futureMatches.Count} future matches for {leagueName} season {season}");
                        
                        ViewBag.LeagueName = leagueName;
                        ViewBag.Season = season;
                        return View("League", futureMatches);
                    }
                    else
                    {
                        _logger.LogWarning($"No matches found for {leagueName} season {season}");
                    }
                }

                // Ако стигнем тук, не сме намерили мачове в никой от сезоните
                ViewBag.Error = $"❌ Няма налични мачове за {leagueName} за сезоните {string.Join(", ", seasonsToTry)}.";
                return View("League", new List<FootballMatch>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading {leagueName} matches");
                
                ViewBag.Error = $"❌ Грешка при зареждане на {leagueName}: {ex.Message}";
                return View("League", new List<FootballMatch>());
            }
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
        [JsonProperty("errors")]
        public object errors { get; set; }
        public int results { get; set; }
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
