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
                        int oldPoints = prediction.Points;
                        int newPoints = 0;

                        if (prediction.PredictedHomeScore == match.HomeScore &&
                            prediction.PredictedAwayScore == match.AwayScore)
                        {
                            newPoints = 10; // Точно познат резултат (Променено на 10 за по-голям бонус)
                        }
                        else if (
                            (match.HomeScore > match.AwayScore && prediction.PredictedHomeScore > prediction.PredictedAwayScore) ||
                            (match.HomeScore < match.AwayScore && prediction.PredictedHomeScore < prediction.PredictedAwayScore) ||
                            (match.HomeScore == match.AwayScore && prediction.PredictedHomeScore == prediction.PredictedAwayScore)
                        )
                        {
                            newPoints = 5; // Познат изход (победа/загуба/равен)
                        }

                        // Обновяване на "портфейла" на потребителя
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
                        ViewBag.LeagueId = leagueId;
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

        // 🇫🇷 Ligue 1
        public async Task<IActionResult> Ligue1()
        {
            return await LoadLeagueMatches("Ligue 1", 61);
        }

        // 🇵🇹 Primeira Liga
        public async Task<IActionResult> PrimeiraLiga()
        {
            return await LoadLeagueMatches("Primeira Liga", 94);
        }

        // 🇳🇱 Eredivisie
        public async Task<IActionResult> Eredivisie()
        {
            return await LoadLeagueMatches("Eredivisie", 88);
        }

        // 🇧🇬 Parva Liga
        public async Task<IActionResult> ParvaLiga()
        {
            return await LoadLeagueMatches("Parva Liga", 172);
        }

        // 📋 Детайли за мач от API (статистика + съставите + стадион + класация + H2H)
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

                // 1️⃣ Стадион — от детайлите на мача
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

                        // Обновяваме статуса до актуален
                        if (detail.fixture.status != null && !string.IsNullOrEmpty(detail.fixture.status.short_))
                        {
                            vm.Status = detail.fixture.status.short_;
                        }

                        // Обновяваме резултата в реално време
                        if (detail.goals != null)
                        {
                            vm.HomeScore = detail.goals.home;
                            vm.AwayScore = detail.goals.away;
                        }
                    }
                }

                // 2️⃣ Съставите
                var lineupsUrl = $"https://v3.football.api-sports.io/fixtures/lineups?fixture={fixtureId}";
                var lineupsResponse = await client.GetAsync(lineupsUrl);
                if (lineupsResponse.IsSuccessStatusCode)
                {
                    var lineupsJson = await lineupsResponse.Content.ReadAsStringAsync();
                    var lineupsResult = JsonConvert.DeserializeObject<LineupsApiResponse>(lineupsJson);
                    vm.Lineups = lineupsResult?.response ?? new List<TeamLineup>();
                }

                // 3️⃣ Статистика
                var statsUrl = $"https://v3.football.api-sports.io/fixtures/statistics?fixture={fixtureId}";
                var statsResponse = await client.GetAsync(statsUrl);
                if (statsResponse.IsSuccessStatusCode)
                {
                    var statsJson = await statsResponse.Content.ReadAsStringAsync();
                    var statsResult = JsonConvert.DeserializeObject<StatisticsApiResponse>(statsJson);
                    vm.Statistics = statsResult?.response ?? new List<TeamStatistics>();
                }

                // 4️⃣ Класация на лигата
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

                // 5️⃣ H2H — последните 10 мача между двата отбора
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

                // 6️⃣ Fetch User Prediction if exists
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    // Най-сигурното търсене: по FixtureId
                    var dbMatch = await _context.Match.FirstOrDefaultAsync(m => m.FixtureId == fixtureId);
                    
                    // Резервен вариант по имена (ако датата се разминава заради часовата зона или FixtureId липсва)
                    if (dbMatch == null)
                    {
                        dbMatch = await _context.Match.FirstOrDefaultAsync(m => 
                            m.HomeTeam == homeTeam && 
                            m.AwayTeam == awayTeam);
                            
                        // Ако сме намерили мача по стари данни, го "ъпгрейдваме" с новия FixtureId за бъдещето
                        if (dbMatch != null && dbMatch.FixtureId == null)
                        {
                            dbMatch.FixtureId = fixtureId;
                            await _context.SaveChangesAsync();
                        }
                    }
                        
                    if (dbMatch != null)
                    {
                        vm.UserPrediction = await _context.Prediction.FirstOrDefaultAsync(p => 
                            p.MatchId == dbMatch.Id && p.UserId == user.Id);
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

        // 🏆 Пълна класация на лигата
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
                    
                    // Проверка за API грешки
                    if (standResult != null && standResult.errors != null)
                    {
                        var errorsJson = JsonConvert.SerializeObject(standResult.errors);
                        if (errorsJson.Contains("requests") || errorsJson.Contains("plan"))
                        {
                            ViewBag.Error = "❌ Превишен лимит на API заявки или грешка в плана. Моля опитай пак по-късно.";
                            return View(vm);
                        }
                    }

                    var leagueData = standResult?.response?.FirstOrDefault()?.league;
                    
                    if (leagueData != null && leagueData.standings != null && leagueData.standings.Any())
                    {
                        vm.Standings = leagueData.standings.FirstOrDefault() ?? new List<StandingEntry>();
                        vm.LeagueLogo = leagueData.logo;
                        // Ако имаме държава като стринг, може да оставим флага празен или да го вземем от друго място
                        // За момента спираме грешката при десериализация
                        vm.Flag = null; 
                    }
                    else
                    {
                        ViewBag.Error = "❌ Няма намерени данни за класацията за този сезон.";
                    }
                }
                else
                {
                    ViewBag.Error = $"❌ Грешка от API: {standResponse.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading standings for league {LeagueId}", leagueId);
                ViewBag.Error = $"❌ Системна грешка: {ex.Message}";
            }

            return View(vm);
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

    // 📋 ViewModel за детайлната страница на мач
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
        // Стадион
        public string VenueName { get; set; }
        public string VenueCity { get; set; }
        // Съставите & Статистика
        public List<TeamLineup> Lineups { get; set; } = new();
        public List<TeamStatistics> Statistics { get; set; } = new();
        // Класация
        public List<StandingEntry> Standings { get; set; } = new();
        public StandingEntry HomeStanding { get; set; }
        public StandingEntry AwayStanding { get; set; }
        // H2H
        public List<FootballMatch> H2HMatches { get; set; } = new();
        public PredictLeague.Models.Prediction? UserPrediction { get; set; }
        public string ApiError { get; set; }
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

    // 📋 Модели за Lineups API
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

    // 📊 Модели за Statistics API
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

    // 🏟️ Venue модел
    public class Venue
    {
        public int? id { get; set; }
        public string name { get; set; }
        public string city { get; set; }
    }

    // Fixture Detail (вкл. venue)
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
    }

    public class FixtureDetailApiResponse
    {
        public List<FixtureDetailMatch> response { get; set; }
    }

    // 📊 Standings модели
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
        public string country { get; set; } // API връща стринг "Spain", не обект
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
