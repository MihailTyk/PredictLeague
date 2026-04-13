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
using Microsoft.AspNetCore.Identity;

namespace PredictLeague.Controllers
{
    public class MatchesController : Controller
    {
        private readonly PredictLeagueContext _context;
        private readonly ILogger<MatchesController> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        public MatchesController(PredictLeagueContext context, ILogger<MatchesController> logger, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        // рџЏџпёЏ Р’СЃРёС‡РєРё РјР°С‡РѕРІРµ вЂ” РґРѕСЃС‚СЉРїРЅРѕ Р·Р° РІСЃРёС‡РєРё
        public IActionResult Index()
        {
            return View();
        }

        // рџ”Ќ Р”РµС‚Р°Р№Р»Рё Р·Р° РјР°С‡ вЂ” РґРѕСЃС‚СЉРїРЅРѕ Р·Р° РІСЃРёС‡РєРё
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var match = await _context.Match.FirstOrDefaultAsync(m => m.Id == id);
            if (match == null)
                return NotFound();

            return View(match);
        }

        // вћ• РЎСЉР·РґР°РІР°РЅРµ РЅР° РЅРѕРІ РјР°С‡ вЂ” СЃР°РјРѕ Р·Р° Admin
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // рџ’ѕ РЎСЉР·РґР°РІР°РЅРµ (POST) вЂ” СЃР°РјРѕ Р·Р° Admin
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

        // вњЏпёЏ Р РµРґР°РєС†РёСЏ РЅР° РјР°С‡ вЂ” СЃР°РјРѕ Р·Р° Admin
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

        // рџ’ѕ Р—Р°РїРёСЃРІР°РЅРµ РЅР° СЂРµРґР°РєС†РёСЏС‚Р° вЂ” СЃР°РјРѕ Р·Р° Admin
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

                    // рџЏ† РР·С‡РёСЃР»СЏРІР°РјРµ С‚РѕС‡РєРёС‚Рµ СЃР»РµРґ РїСЂРёРєР»СЋС‡РІР°РЅРµ РЅР° РјР°С‡Р°
                    var predictions = await _context.Prediction.Where(p => p.MatchId == match.Id).ToListAsync();

                    foreach (var prediction in predictions)
                    {
                        int oldPoints = prediction.Points;
                        int newPoints = 0;

                        if (prediction.PredictedHomeScore == match.HomeScore &&
                            prediction.PredictedAwayScore == match.AwayScore)
                        {
                            newPoints = 10; // РўРѕС‡РЅРѕ РїРѕР·РЅР°С‚ СЂРµР·СѓР»С‚Р°С‚ (РџСЂРѕРјРµРЅРµРЅРѕ РЅР° 10 Р·Р° РїРѕ-РіРѕР»СЏРј Р±РѕРЅСѓСЃ)
                        }
                        else if (
                            (match.HomeScore > match.AwayScore && prediction.PredictedHomeScore > prediction.PredictedAwayScore) ||
                            (match.HomeScore < match.AwayScore && prediction.PredictedHomeScore < prediction.PredictedAwayScore) ||
                            (match.HomeScore == match.AwayScore && prediction.PredictedHomeScore == prediction.PredictedAwayScore)
                        )
                        {
                            newPoints = 5; // РџРѕР·РЅР°С‚ РёР·С…РѕРґ (РїРѕР±РµРґР°/Р·Р°РіСѓР±Р°/СЂР°РІРµРЅ)
                        }

                        // РћР±РЅРѕРІСЏРІР°РЅРµ РЅР° "РїРѕСЂС‚С„РµР№Р»Р°" РЅР° РїРѕС‚СЂРµР±РёС‚РµР»СЏ
                        if (newPoints != oldPoints)
                        {
                            var userSettings = await _context.UserTeamSettings.FirstOrDefaultAsync(s => s.UserId == prediction.UserId);
                            if (userSettings != null)
                            {
                                userSettings.Points += (newPoints - oldPoints);
                                _context.Update(userSettings);
                            }
                        }

                        prediction.Points = newPoints;
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

        // вќЊ РР·С‚СЂРёРІР°РЅРµ РЅР° РјР°С‡ вЂ” СЃР°РјРѕ Р·Р° Admin
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

        // рџ’Ј РџРѕС‚РІСЉСЂР¶РґРµРЅРёРµ РЅР° РёР·С‚СЂРёРІР°РЅРµС‚Рѕ вЂ” СЃР°РјРѕ Р·Р° Admin
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

        // вљЅ РЈРЅРёРІРµСЂСЃР°Р»РµРЅ РјРµС‚РѕРґ Р·Р° Р·Р°СЂРµР¶РґР°РЅРµ РЅР° РјР°С‡РѕРІРµ РїРѕ Р»РёРіР°
        private async Task<IActionResult> LoadLeagueMatches(string leagueName, int leagueId)
        {
            try
            {
                var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                string apiKey = configuration["ApiKeys:ApiSports"] ?? "";
                
                // Р‘РµР·РїР»Р°С‚РЅРёСЏС‚ РїР»Р°РЅ РЅР° API-Sports.io (Р°РєРѕ РёРјР° РїСЂРѕРјСЏРЅР°) РёР»Рё РЅРѕСЂРјР°Р»РµРЅ РєР»СЋС‡
                // РР·РїРѕР»Р·РІР°РјРµ РЅР°Р№-РЅРѕРІРёС‚Рµ РґРѕСЃС‚СЉРїРЅРё СЃРµР·РѕРЅРё СЃРїСЂСЏРјРѕ 2026-С‚Р° РіРѕРґРёРЅР°
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
                            ViewBag.Error = $"вќЊ API РєР»СЋС‡СЉС‚ Рµ РЅРµРІР°Р»РёРґРµРЅ РёР»Рё РёР·С‚РµРєСЉР». РџСЂРѕРІРµСЂРё API РєР»СЋС‡Р° Р·Р° {leagueName}.";
                            return View("League", new List<FootballMatch>());
                        }
                        
                        // РћРїРёС‚Р°Р№ СЃР»РµРґРІР°С‰РёСЏ СЃРµР·РѕРЅ
                        continue;
                    }

                    json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Response received for season {season}. Length: {json.Length} characters");
                    
                    // Р›РѕРіРІР°РјРµ РїСЉСЂРІРёС‚Рµ 500 СЃРёРјРІРѕР»Р° Р·Р° РґРµР±СЉРіРІР°РЅРµ
                    if (json.Length > 0)
                    {
                        _logger.LogInformation($"Response preview (first 500 chars): {json.Substring(0, Math.Min(500, json.Length))}");
                    }
                    
                    result = JsonConvert.DeserializeObject<FootballApiResponse>(json);

                    // РџСЂРѕРІРµСЂСЏРІР°РјРµ РґР°Р»Рё РёРјР° errors РІ response-Р°
                    if (result != null && result.errors != null)
                    {
                        string errorMessage = "";
                        
                        // РћР±СЂР°Р±РѕС‚РІР°РјРµ errors РєР°С‚Рѕ РѕР±РµРєС‚ РёР»Рё РјР°СЃРёРІ
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
                            
                            // РђРєРѕ РіСЂРµС€РєР°С‚Р° Рµ Р·Р° РїР»Р°РЅР°, РїРѕРєР°Р·РІР°РјРµ РїРѕ-СЏСЃРЅРѕ СЃСЉРѕР±С‰РµРЅРёРµ
                            if (errorMessage.Contains("Free plans") || errorMessage.Contains("plan"))
                            {
                                _logger.LogInformation($"Skipping season {season} due to plan restrictions");
                                continue; // РћРїРёС‚Р°Р№ СЃР»РµРґРІР°С‰РёСЏ СЃРµР·РѕРЅ
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
                        ViewBag.LeagueId = leagueId;
                        return View("League", futureMatches);
                    }
                    else
                    {
                        _logger.LogWarning($"No matches found for {leagueName} season {season}");
                    }
                }

                // РђРєРѕ СЃС‚РёРіРЅРµРј С‚СѓРє, РЅРµ СЃРјРµ РЅР°РјРµСЂРёР»Рё РјР°С‡РѕРІРµ РІ РЅРёРєРѕР№ РѕС‚ СЃРµР·РѕРЅРёС‚Рµ
                ViewBag.Error = $"вќЊ РќСЏРјР° РЅР°Р»РёС‡РЅРё РјР°С‡РѕРІРµ Р·Р° {leagueName} Р·Р° СЃРµР·РѕРЅРёС‚Рµ {string.Join(", ", seasonsToTry)}.";
                return View("League", new List<FootballMatch>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading {leagueName} matches");
                
                ViewBag.Error = $"вќЊ Р“СЂРµС€РєР° РїСЂРё Р·Р°СЂРµР¶РґР°РЅРµ РЅР° {leagueName}: {ex.Message}";
                return View("League", new List<FootballMatch>());
            }
        }

        // рџЏґ Premier League
        [Authorize]
        public async Task<IActionResult> PremierLeague()
        {
            return await LoadLeagueMatches("Premier League", 39);
        }

        // рџ‡Єрџ‡ё La Liga
        [Authorize]
        public async Task<IActionResult> LaLiga()
        {
            return await LoadLeagueMatches("La Liga", 140);
        }

        // рџ‡®рџ‡№ Serie A
        [Authorize]
        public async Task<IActionResult> SerieA()
        {
            return await LoadLeagueMatches("Serie A", 135);
        }

        // рџ‡©рџ‡Є Bundesliga
        [Authorize]
        public async Task<IActionResult> Bundesliga()
        {
            return await LoadLeagueMatches("Bundesliga", 78);
        }

        // рџЏ† Champions League
        [Authorize]
        public async Task<IActionResult> ChampionsLeague()
        {
            return await LoadLeagueMatches("Champions League", 2);
        }

        // рџ‡«рџ‡· Ligue 1
        [Authorize]
        public async Task<IActionResult> Ligue1()
        {
            return await LoadLeagueMatches("Ligue 1", 61);
        }

        // рџ‡µрџ‡№ Primeira Liga
        [Authorize]
        public async Task<IActionResult> PrimeiraLiga()
        {
            return await LoadLeagueMatches("Primeira Liga", 94);
        }

        // рџ‡ірџ‡± Eredivisie
        [Authorize]
        public async Task<IActionResult> Eredivisie()
        {
            return await LoadLeagueMatches("Eredivisie", 88);
        }

        // рџ‡§рџ‡¬ Parva Liga
        [Authorize]
        public async Task<IActionResult> ParvaLiga()
        {
            return await LoadLeagueMatches("Parva Liga", 172);
        }

        // рџ“‹ Р”РµС‚Р°Р№Р»Рё Р·Р° РјР°С‡ РѕС‚ API (СЃС‚Р°С‚РёСЃС‚РёРєР° + СЃСЉСЃС‚Р°РІРёС‚Рµ + СЃС‚Р°РґРёРѕРЅ + РєР»Р°СЃР°С†РёСЏ + H2H)
        public async Task<IActionResult> MatchDetail(
            int fixtureId, int homeTeamId, int awayTeamId,
            string homeTeam, string awayTeam, string homeLogo, string awayLogo,
            string matchDate, string status, int leagueId = 0, int season = 0)
        {
            var vm = new MatchDetailViewModel
            {
                FixtureId = fixtureId,
                HomeTeamId = homeTeamId,
                AwayTeamId = awayTeamId,
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                HomeLogo = homeLogo,
                AwayLogo = awayLogo,
                MatchDate = matchDate,
                Status = status,
                LeagueId = leagueId,
                Season = season
            };

            try
            {
                var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                string apiKey = configuration["ApiKeys:ApiSports"] ?? "";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-apisports-key", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(30);

                // 1пёЏвѓЈ РЎС‚Р°РґРёРѕРЅ вЂ” РѕС‚ РґРµС‚Р°Р№Р»РёС‚Рµ РЅР° РјР°С‡Р°
                var fixtureUrl = $"https://v3.football.api-sports.io/fixtures?id={fixtureId}";
                var fixtureResponse = await client.GetAsync(fixtureUrl);
                if (fixtureResponse.IsSuccessStatusCode)
                {
                    var fixtureJson = await fixtureResponse.Content.ReadAsStringAsync();
                    var fixtureResult = JsonConvert.DeserializeObject<FixtureDetailApiResponse>(fixtureJson);
                    var detail = fixtureResult?.response?.FirstOrDefault();
                    if (detail?.fixture != null)
                    {
                        if (detail.fixture.venue != null)
                        {
                            vm.VenueName = detail.fixture.venue.name;
                            vm.VenueCity = detail.fixture.venue.city;
                        }

                        // РћР±РЅРѕРІСЏРІР°РјРµ СЃС‚Р°С‚СѓСЃР° РґРѕ Р°РєС‚СѓР°Р»РµРЅ
                        if (detail.fixture.status != null && !string.IsNullOrEmpty(detail.fixture.status.short_))
                        {
                            vm.Status = detail.fixture.status.short_;
                        }

                        // РћР±РЅРѕРІСЏРІР°РјРµ СЂРµР·СѓР»С‚Р°С‚Р° РІ СЂРµР°Р»РЅРѕ РІСЂРµРјРµ
                        if (detail.goals != null && detail.goals.home != null && detail.goals.away != null)
                        {
                            vm.HomeScore = detail.goals.home;
                            vm.AwayScore = detail.goals.away;

                            // Р—Р°РїРёСЃРІР°РјРµ РІ Р±Р°Р·Р°С‚Р°, Р°РєРѕ РёРјР° РїСЂРѕРјСЏРЅР° РёР»Рё Рµ РїСЂРёРєР»СЋС‡РёР»
                            string matchStatus = detail.fixture.status?.short_;
                            bool isFinished = (matchStatus == "FT" || matchStatus == "AET" || matchStatus == "PEN");

                            var dbMatch = await _context.Match.FirstOrDefaultAsync(m => m.FixtureId == fixtureId);
                            if (dbMatch != null && (!dbMatch.IsFinished && isFinished))
                            {
                                dbMatch.HomeScore = detail.goals.home;
                                dbMatch.AwayScore = detail.goals.away;
                                dbMatch.IsFinished = isFinished;

                                // Р”СѓР·РїРё
                                if (detail.events != null) { dbMatch.HadPenalty = detail.events.Any(e => !string.IsNullOrEmpty(e.detail) && e.detail.Contains("Penalty", StringComparison.OrdinalIgnoreCase)); var scorers = detail.events.Where(e => e.type == "Goal" && e.player != null && !string.IsNullOrEmpty(e.player.name)).Select(e => e.player.name).Distinct(); dbMatch.ActualGoalscorers = string.Join(", ", scorers); } if (vm.Statistics != null && vm.Statistics.Count >= 1) { int totalCorners = 0, totalOffsides = 0, totalYellow = 0, totalRed = 0; foreach (var teamStat in vm.Statistics) { foreach (var stat in teamStat.statistics) { int val = 0; int.TryParse(stat.value?.ToString(), out val); if (stat.type == "Corner Kicks") totalCorners += val; if (stat.type == "Offsides") totalOffsides += val; if (stat.type == "Yellow Cards") totalYellow += val; if (stat.type == "Red Cards") totalRed += val; } } dbMatch.ActualCorners = totalCorners; dbMatch.ActualOffsides = totalOffsides; dbMatch.ActualYellowCards = totalYellow; dbMatch.ActualRedCards = totalRed; }
                                _context.Update(dbMatch);

                                // РћР±РЅРѕРІСЏРІР°РјРµ С‚РѕС‡РєРёС‚Рµ Р·Р° РІСЃРёС‡РєРё РїСЂРµРґСЃРєР°Р·Р°РЅРёСЏ Р·Р° С‚РѕР·Рё РјР°С‡
                                var predictions = await _context.Prediction.Where(p => p.MatchId == dbMatch.Id).ToListAsync();
                                foreach (var prediction in predictions)
                                {
                                    int oldPoints = prediction.Points;
                                    int newPoints = 0;

                                    if (prediction.PredictedHomeScore == dbMatch.HomeScore &&
                                        prediction.PredictedAwayScore == dbMatch.AwayScore)
                                    {
                                        newPoints = 10;
                                    }
                                    else 
                                    {
                                        if (prediction.PredictedHomeScore == dbMatch.HomeScore) newPoints += 3;
                                        if (prediction.PredictedAwayScore == dbMatch.AwayScore) newPoints += 3;

                                        // 1 С‚РѕС‡РєР° Р·Р° РїРѕР·РЅР°С‚ Р·РЅР°Рє (РїРѕР±РµРґРёС‚РµР»/СЂР°РІРµРЅ), Р°РєРѕ РЅСЏРјР° РїРѕР·РЅР°С‚Рё РіРѕР»РѕРІРµ
                                        if (newPoints == 0)
                                        {
                                            bool outcomeMatches = (dbMatch.HomeScore > dbMatch.AwayScore && prediction.PredictedHomeScore > prediction.PredictedAwayScore) ||
                                                                 (dbMatch.HomeScore < dbMatch.AwayScore && prediction.PredictedHomeScore < prediction.PredictedAwayScore) ||
                                                                 (dbMatch.HomeScore == dbMatch.AwayScore && prediction.PredictedHomeScore == prediction.PredictedAwayScore);
                                            if (outcomeMatches) newPoints = 1;
                                        }
                                    }

                                    // Р‘РѕРЅСѓСЃ С‚РѕС‡РєРё Р·Р° РїРѕР·РЅР°С‚Р° РґСѓР·РїР°
                                    if (dbMatch.HadPenalty == true && prediction.PredictedPenalty == true) { newPoints += 3; } if (!string.IsNullOrEmpty(prediction.AnytimeGoalscorer) && !string.IsNullOrEmpty(dbMatch.ActualGoalscorers)) { var scorersList = dbMatch.ActualGoalscorers.Split(", ", StringSplitOptions.RemoveEmptyEntries); if (scorersList.Any(s => s.Contains(prediction.AnytimeGoalscorer, StringComparison.OrdinalIgnoreCase) || prediction.AnytimeGoalscorer.Contains(s, StringComparison.OrdinalIgnoreCase))) newPoints += 5; } if (prediction.PredictedCorners.HasValue && dbMatch.ActualCorners.HasValue && prediction.PredictedCorners == dbMatch.ActualCorners) newPoints += 3; if (prediction.PredictedOffsides.HasValue && dbMatch.ActualOffsides.HasValue && prediction.PredictedOffsides == dbMatch.ActualOffsides) newPoints += 2; if (prediction.PredictedYellowCards.HasValue && dbMatch.ActualYellowCards.HasValue && prediction.PredictedYellowCards == dbMatch.ActualYellowCards) newPoints += 2; if (prediction.PredictedRedCards.HasValue && dbMatch.ActualRedCards.HasValue && prediction.PredictedRedCards == dbMatch.ActualRedCards) newPoints += 2;

                                    if (newPoints != oldPoints)
                                    {
                                        var userSettings = await _context.UserTeamSettings.FirstOrDefaultAsync(s => s.UserId == prediction.UserId);
                                        if (userSettings == null)
                                        {
                                            userSettings = new Models.UserTeamSettings 
                                            { 
                                                UserId = prediction.UserId, 
                                                Points = newPoints, 
                                                Formation = "4-4-2" 
                                            };
                                            _context.UserTeamSettings.Add(userSettings);
                                        }
                                        else
                                        {
                                            userSettings.Points += (newPoints - oldPoints);
                                            _context.Update(userSettings);
                                        }
                                        prediction.Points = newPoints;
                                        _context.Update(prediction);
                                    }
                                }
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                }

                // 2пёЏвѓЈ РЎСЉСЃС‚Р°РІРёС‚Рµ
                var lineupsUrl = $"https://v3.football.api-sports.io/fixtures/lineups?fixture={fixtureId}";
                var lineupsResponse = await client.GetAsync(lineupsUrl);
                if (lineupsResponse.IsSuccessStatusCode)
                {
                    var lineupsJson = await lineupsResponse.Content.ReadAsStringAsync();
                    var lineupsResult = JsonConvert.DeserializeObject<LineupsApiResponse>(lineupsJson);
                    vm.Lineups = lineupsResult?.response ?? new List<TeamLineup>();
                }

                // 3пёЏвѓЈ РЎС‚Р°С‚РёСЃС‚РёРєР°
                var statsUrl = $"https://v3.football.api-sports.io/fixtures/statistics?fixture={fixtureId}";
                var statsResponse = await client.GetAsync(statsUrl);
                if (statsResponse.IsSuccessStatusCode)
                {
                    var statsJson = await statsResponse.Content.ReadAsStringAsync();
                    var statsResult = JsonConvert.DeserializeObject<StatisticsApiResponse>(statsJson);
                    vm.Statistics = statsResult?.response ?? new List<TeamStatistics>();
                }

                // 4пёЏвѓЈ РљР»Р°СЃР°С†РёСЏ РЅР° Р»РёРіР°С‚Р°
                if (leagueId > 0 && season > 0)
                {
                    var standUrl = $"https://v3.football.api-sports.io/standings?league={leagueId}&season={season}";
                    var standResponse = await client.GetAsync(standUrl);
                    if (standResponse.IsSuccessStatusCode)
                    {
                        var standJson = await standResponse.Content.ReadAsStringAsync();
                        var standResult = JsonConvert.DeserializeObject<StandingsApiResponse>(standJson);
                        var standings = standResult?.response?.FirstOrDefault()?.league?.standings?.FirstOrDefault();
                        if (standings != null)
                        {
                            vm.Standings = standings;
                            vm.HomeStanding = standings.FirstOrDefault(s => s.team?.id == homeTeamId);
                            vm.AwayStanding = standings.FirstOrDefault(s => s.team?.id == awayTeamId);
                        }
                    }
                }

                // 5пёЏвѓЈ H2H вЂ” РїРѕСЃР»РµРґРЅРёС‚Рµ 10 РјР°С‡Р° РјРµР¶РґСѓ РґРІР°С‚Р° РѕС‚Р±РѕСЂР°
                if (homeTeamId > 0 && awayTeamId > 0)
                {
                    var h2hUrl = $"https://v3.football.api-sports.io/fixtures/headtohead?h2h={homeTeamId}-{awayTeamId}&last=10";
                    var h2hResponse = await client.GetAsync(h2hUrl);
                    if (h2hResponse.IsSuccessStatusCode)
                    {
                        var h2hJson = await h2hResponse.Content.ReadAsStringAsync();
                        var h2hResult = JsonConvert.DeserializeObject<FootballApiResponse>(h2hJson);
                        vm.H2HMatches = h2hResult?.response ?? new List<FootballMatch>();
                    }
                }

                // 6пёЏвѓЈ Fetch User Prediction if exists
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    // РќР°Р№-СЃРёРіСѓСЂРЅРѕС‚Рѕ С‚СЉСЂСЃРµРЅРµ: РїРѕ FixtureId
                    var dbMatch = await _context.Match.FirstOrDefaultAsync(m => m.FixtureId == fixtureId);
                    
                    // Р РµР·РµСЂРІРµРЅ РІР°СЂРёР°РЅС‚ РїРѕ РёРјРµРЅР° (Р°РєРѕ РґР°С‚Р°С‚Р° СЃРµ СЂР°Р·РјРёРЅР°РІР° Р·Р°СЂР°РґРё С‡Р°СЃРѕРІР°С‚Р° Р·РѕРЅР° РёР»Рё FixtureId Р»РёРїСЃРІР°)
                    if (dbMatch == null)
                    {
                        dbMatch = await _context.Match.FirstOrDefaultAsync(m => 
                            m.HomeTeam == homeTeam && 
                            m.AwayTeam == awayTeam);
                            
                        // РђРєРѕ СЃРјРµ РЅР°РјРµСЂРёР»Рё РјР°С‡Р° РїРѕ СЃС‚Р°СЂРё РґР°РЅРЅРё, РіРѕ "СЉРїРіСЂРµР№РґРІР°РјРµ" СЃ РЅРѕРІРёСЏ FixtureId Р·Р° Р±СЉРґРµС‰РµС‚Рѕ
                        if (dbMatch != null && dbMatch.FixtureId == null)
                        {
                            dbMatch.FixtureId = fixtureId;
                            await _context.SaveChangesAsync();
                        }
                    }
                        
                    if (dbMatch != null)
                    {
                        vm.HadPenalty = dbMatch.HadPenalty;
                        vm.UserPrediction = await _context.Prediction
                            .Where(p => p.MatchId == dbMatch.Id && p.UserId == user.Id)
                            .OrderByDescending(p => p.CreatedAt)
                            .FirstOrDefaultAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading match detail for fixture {FixtureId}", fixtureId);
                vm.ApiError = ex.Message;
            }

            return View("MatchDetail", vm);
        }

        // рџЏ† РџСЉР»РЅР° РєР»Р°СЃР°С†РёСЏ РЅР° Р»РёРіР°С‚Р°
        public async Task<IActionResult> Standings(int leagueId, int season, string leagueName)
        {
            var vm = new FullStandingsViewModel
            {
                LeagueId = leagueId,
                Season = season,
                LeagueName = leagueName
            };

            try
            {
                var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                string apiKey = configuration["ApiKeys:ApiSports"] ?? "";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-apisports-key", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(30);

                var standUrl = $"https://v3.football.api-sports.io/standings?league={leagueId}&season={season}";
                var standResponse = await client.GetAsync(standUrl);
                
                if (standResponse.IsSuccessStatusCode)
                {
                    var standJson = await standResponse.Content.ReadAsStringAsync();
                    var standResult = JsonConvert.DeserializeObject<StandingsApiResponse>(standJson);
                    
                    // РџСЂРѕРІРµСЂРєР° Р·Р° API РіСЂРµС€РєРё
                    if (standResult != null && standResult.errors != null)
                    {
                        var errorsJson = JsonConvert.SerializeObject(standResult.errors);
                        if (errorsJson.Contains("requests") || errorsJson.Contains("plan"))
                        {
                            ViewBag.Error = "вќЊ РџСЂРµРІРёС€РµРЅ Р»РёРјРёС‚ РЅР° API Р·Р°СЏРІРєРё РёР»Рё РіСЂРµС€РєР° РІ РїР»Р°РЅР°. РњРѕР»СЏ РѕРїРёС‚Р°Р№ РїР°Рє РїРѕ-РєСЉСЃРЅРѕ.";
                            return View(vm);
                        }
                    }

                    var leagueData = standResult?.response?.FirstOrDefault()?.league;
                    
                    if (leagueData != null && leagueData.standings != null && leagueData.standings.Any())
                    {
                        vm.Standings = leagueData.standings.FirstOrDefault() ?? new List<StandingEntry>();
                        vm.LeagueLogo = leagueData.logo;
                        // РђРєРѕ РёРјР°РјРµ РґСЉСЂР¶Р°РІР° РєР°С‚Рѕ СЃС‚СЂРёРЅРі, РјРѕР¶Рµ РґР° РѕСЃС‚Р°РІРёРј С„Р»Р°РіР° РїСЂР°Р·РµРЅ РёР»Рё РґР° РіРѕ РІР·РµРјРµРј РѕС‚ РґСЂСѓРіРѕ РјСЏСЃС‚Рѕ
                        // Р—Р° РјРѕРјРµРЅС‚Р° СЃРїРёСЂР°РјРµ РіСЂРµС€РєР°С‚Р° РїСЂРё РґРµСЃРµСЂРёР°Р»РёР·Р°С†РёСЏ
                        vm.Flag = null; 
                    }
                    else
                    {
                        ViewBag.Error = "вќЊ РќСЏРјР° РЅР°РјРµСЂРµРЅРё РґР°РЅРЅРё Р·Р° РєР»Р°СЃР°С†РёСЏС‚Р° Р·Р° С‚РѕР·Рё СЃРµР·РѕРЅ.";
                    }
                }
                else
                {
                    ViewBag.Error = $"вќЊ Р“СЂРµС€РєР° РѕС‚ API: {standResponse.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading standings for league {LeagueId}", leagueId);
                ViewBag.Error = $"вќЊ РЎРёСЃС‚РµРјРЅР° РіСЂРµС€РєР°: {ex.Message}";
            }

            return View(vm);
        }
    }

    // рџ§© РњРѕРґРµР»Рё Р·Р° Football API
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
        public int id { get; set; }
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
        public int id { get; set; }
        public string name { get; set; }
        public string logo { get; set; }
    }

    public class Goals
    {
        public int? home { get; set; }
        public int? away { get; set; }
    }

    // рџ“‹ ViewModel Р·Р° РґРµС‚Р°Р№Р»РЅР°С‚Р° СЃС‚СЂР°РЅРёС†Р° РЅР° РјР°С‡
    public class MatchDetailViewModel
    {
        public int FixtureId { get; set; }
        public int HomeTeamId { get; set; }
        public int AwayTeamId { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public string HomeLogo { get; set; }
        public string AwayLogo { get; set; }
        public string MatchDate { get; set; }
        public string Status { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public int LeagueId { get; set; }
        public int Season { get; set; }
        // РЎС‚Р°РґРёРѕРЅ
        public string VenueName { get; set; }
        public string VenueCity { get; set; }
        // РЎСЉСЃС‚Р°РІРёС‚Рµ & РЎС‚Р°С‚РёСЃС‚РёРєР°
        public List<TeamLineup> Lineups { get; set; } = new();
        public List<TeamStatistics> Statistics { get; set; } = new();
        // РљР»Р°СЃР°С†РёСЏ
        public List<StandingEntry> Standings { get; set; } = new();
        public StandingEntry HomeStanding { get; set; }
        public StandingEntry AwayStanding { get; set; }
        // H2H
        public List<FootballMatch> H2HMatches { get; set; } = new();
        public PredictLeague.Models.Prediction? UserPrediction { get; set; }
        public string ApiError { get; set; }
        public bool? HadPenalty { get; set; }
    }

    public class FullStandingsViewModel
    {
        public int LeagueId { get; set; }
        public int Season { get; set; }
        public string LeagueName { get; set; }
        public string LeagueLogo { get; set; }
        public string Flag { get; set; }
        public List<StandingEntry> Standings { get; set; } = new();
    }

    // рџ“‹ РњРѕРґРµР»Рё Р·Р° Lineups API
    public class LineupsApiResponse
    {
        public List<TeamLineup> response { get; set; }
    }

    public class TeamLineup
    {
        public LineupTeam team { get; set; }
        public string formation { get; set; }
        public List<LineupPlayer> startXI { get; set; } = new();
        public List<LineupPlayer> substitutes { get; set; } = new();
        public LineupCoach coach { get; set; }
    }

    public class LineupTeam
    {
        public int id { get; set; }
        public string name { get; set; }
        public string logo { get; set; }
        public object colors { get; set; }
    }

    public class LineupPlayer
    {
        public LineupPlayerInfo player { get; set; }
    }

    public class LineupPlayerInfo
    {
        public int id { get; set; }
        public string name { get; set; }
        public int? number { get; set; }
        public string pos { get; set; }
        public string grid { get; set; }
    }

    public class LineupCoach
    {
        public int id { get; set; }
        public string name { get; set; }
        public string photo { get; set; }
    }

    // рџ“Љ РњРѕРґРµР»Рё Р·Р° Statistics API
    public class StatisticsApiResponse
    {
        public List<TeamStatistics> response { get; set; }
    }

    public class TeamStatistics
    {
        public LineupTeam team { get; set; }
        public List<StatisticItem> statistics { get; set; } = new();
    }

    public class StatisticItem
    {
        public string type { get; set; }
        public object value { get; set; }
    }

    // рџЏџпёЏ Venue РјРѕРґРµР»
    public class Venue
    {
        public int? id { get; set; }
        public string name { get; set; }
        public string city { get; set; }
    }

    // Fixture Detail (РІРєР». venue)
    public class FixtureDetail
    {
        public int id { get; set; }
        public Venue venue { get; set; }
        public DateTime date { get; set; }
        public Status status { get; set; }
    }

    public class FixtureDetailMatch
    {
        public FixtureDetail fixture { get; set; }
        public Teams teams { get; set; }
        public Goals goals { get; set; }
        public List<FixtureEvent> events { get; set; }
    }

    public class FixtureEvent
    {
        public string type { get; set; }
        public string detail { get; set; }
        public PlayerEventInfo player { get; set; }
    }

    public class PlayerEventInfo
    {
        public int? id { get; set; }
        public string name { get; set; }
    }

    public class FixtureDetailApiResponse
    {
        public List<FixtureDetailMatch> response { get; set; }
    }

    // рџ“Љ Standings РјРѕРґРµР»Рё
    public class StandingsApiResponse
    {
        public List<StandingsResponseItem> response { get; set; }
        [JsonProperty("errors")]
        public object errors { get; set; }
    }

    public class StandingsResponseItem
    {
        public LeagueStandings league { get; set; }
    }

    public class LeagueStandings
    {
        public int id { get; set; }
        public string name { get; set; }
        public string logo { get; set; }
        public string country { get; set; } // API РІСЂСЉС‰Р° СЃС‚СЂРёРЅРі "Spain", РЅРµ РѕР±РµРєС‚
        public List<List<StandingEntry>> standings { get; set; }
    }

    public class Country
    {
        public string name { get; set; }
        public string flag { get; set; }
    }

    public class StandingEntry
    {
        public int? rank { get; set; }
        public Team team { get; set; }
        public int? points { get; set; }
        public int? goalsDiff { get; set; }
        public string form { get; set; }
        public string description { get; set; }
        public StandingStats all { get; set; }
    }

    public class StandingStats
    {
        public int played { get; set; }
        public int win { get; set; }
        public int draw { get; set; }
        public int lose { get; set; }
        public StandingGoals goals { get; set; }
    }

    public class StandingGoals
    {
        [JsonProperty("for")]
        public int goalsFor { get; set; }
        public int against { get; set; }
    }
}




