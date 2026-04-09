using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PredictLeague.Controllers
{
    public class PlayersController : Controller
    {
        private readonly ILogger<PlayersController> _logger;
        private readonly Data.PredictLeagueContext _context;
        private readonly UserManager<Microsoft.AspNetCore.Identity.IdentityUser> _userManager;
        private const string ApiKey = "a1c5c63f7d7b71136b4512647b1da851";

        public PlayersController(ILogger<PlayersController> logger, Data.PredictLeagueContext context, UserManager<Microsoft.AspNetCore.Identity.IdentityUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int leagueId = 39, int season = 2024, int page = 1, string search = null)
        {
            // Списък с поддържани лиги за менюто
            ViewBag.Leagues = new Dictionary<int, string>
            {
                { 39, "Premier League" },
                { 140, "La Liga" },
                { 135, "Serie A" },
                { 78, "Bundesliga" },
                { 2, "Champ. League" },
                { 61, "Ligue 1" },
                { 94, "Primeira Liga" },
                { 88, "Eredivisie" },
                { 172, "Parva Liga" }
            };

            ViewBag.CurrentLeagueId = leagueId;
            ViewBag.CurrentSeason = season;
            ViewBag.CurrentPage = page;
            ViewBag.SearchTerm = search;
            
            ViewBag.CurrentLeagueName = (ViewBag.Leagues as Dictionary<int, string>).ContainsKey(leagueId) 
                ? (ViewBag.Leagues as Dictionary<int, string>)[leagueId] 
                : "League";

            // Get user's existing players to mark them in UI
            var user = await _userManager.GetUserAsync(User);
            HashSet<int> userPlayerIds = new HashSet<int>();
            if (user != null)
            {
                // Ensure we use the correct namespace for ToListAsync or use standard ToList if EF Core namespace is missing, 
                // but better to include it. Since I cannot easily add top-level using without rewriting file, 
                // I will use standard synchronous ToList() or verify if I can add the using.
                // Actually, _context.UserPlayers is a DbSet.
                // I'll stick to synchronous logic for safety or use IQueryable.
                // Or I can add "using Microsoft.EntityFrameworkCore;" at the top in a separate edit.
                // For now, let's use synchronous loop/linq if async is tricky, BUT I need async for Performace.
                // Wait, I modify the method to be `async Task`.
                // I will assume I can add the using directive or fully qualify it.
                // Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(...)
                
                userPlayerIds = (await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                    _context.UserPlayers
                    .Where(up => up.UserId == user.Id)
                    .Select(up => up.PlayerApiId)))
                    .ToHashSet();
            }
            ViewBag.UserPlayerIds = userPlayerIds;

            // Ако има малко търсене (API изисква мин 3 символа), връщаме предупреждение
            if (!string.IsNullOrEmpty(search) && search.Length < 3)
            {
                ViewBag.Error = "Търсенето трябва да съдържа поне 3 символа.";
                return View("Index", new List<PlayerEntry>());
            }

            // 1. Ако НЕ търсим, зареждаме Топ Голмайсторите за най-отгоре
            if (string.IsNullOrEmpty(search) && page == 1)
            {
                ViewBag.TopScorers = await FetchTopScorers(leagueId, season);
            }

            // 2. Зареждаме основния списък (или резултатите от търсенето)
            return await LoadPlayers(leagueId, season, page, search);
        }

        private async Task<List<PlayerEntry>> FetchTopScorers(int leagueId, int season)
        {
            try
            {
                // Взимаме само топ 4 за хедъра
                string url = $"https://v3.football.api-sports.io/players/topscorers?league={leagueId}&season={season}";
                var result = await FetchApiData(url);
                
                if (result == null || result.Response == null || !result.Response.Any())
                {
                    _logger.LogWarning($"Top Scorers API returned no data for League {leagueId} Season {season}");
                    return null;
                }

                return result.Response.Take(4).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Top Scorers");
                return null; 
            }
        }

        private async Task<IActionResult> LoadPlayers(int leagueId, int season, int page, string search)
        {
            try
            {
                string url;
                if (!string.IsNullOrEmpty(search))
                {
                    // Търсене по име
                    url = $"https://v3.football.api-sports.io/players?league={leagueId}&season={season}&search={search}";
                }
                else
                {
                    // Стандартно странициране
                    url = $"https://v3.football.api-sports.io/players?league={leagueId}&season={season}&page={page}";
                }

                var result = await FetchApiData(url);

                if (result != null && result.Errors != null)
                {
                    bool hasErrors = false;
                    if (result.Errors is Newtonsoft.Json.Linq.JArray arr && arr.Count == 0) hasErrors = false;
                    else if (result.Errors is Newtonsoft.Json.Linq.JObject obj && !obj.HasValues) hasErrors = false;
                    else hasErrors = true;

                    if (hasErrors)
                    {
                        ViewBag.Error = "Грешка от API: " + result.Errors.ToString();
                        return View("Index", new List<PlayerEntry>());
                    }
                }

                if (result != null && result.Response != null)
                {
                     // ФИЛТРИРАНЕ: Показваме всички играчи
                     List<PlayerEntry> playersToShow = result.Response;

                     // Запазваме информация за страниците
                     if (result.Paging != null)
                     {
                         ViewBag.TotalPages = result.Paging.Total;
                     }
                     else
                     {
                         ViewBag.TotalPages = 1; // При търсене обикновено няма странициране в този endpoint по същия начин
                     }
                     
                     if (!result.Response.Any() && !string.IsNullOrEmpty(search))
                     {
                         ViewBag.Warning = "Няма намерени играчи с това име. Уверете се, че сте написали името на латиница (напр. 'Yamal' вместо 'Ямал').";
                     }

                     return View("Index", playersToShow);
                }

                return View("Index", new List<PlayerEntry>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching players");
                ViewBag.Error = $"Грешка: {ex.Message}";
                return View("Index", new List<PlayerEntry>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToTeam(int playerId, string playerName, string position, string rating, string teamName)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return LocalRedirect("/Identity/Account/Login");
            }

            // 1. Get or Create User Team Settings
            var teamSettings = _context.UserTeamSettings.FirstOrDefault(s => s.UserId == user.Id);
            if (teamSettings == null)
            {
                teamSettings = new Models.UserTeamSettings { UserId = user.Id, Points = 0, Formation = "4-4-2" };
                _context.UserTeamSettings.Add(teamSettings);
                await _context.SaveChangesAsync();
            }

            // Parse rating FIRST so we calculate dynamic cost
            double ratingValue = 0;
            if (double.TryParse(rating, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double r))
            {
                ratingValue = r;
            }

            // Динамична цена: Рейтинг * 5 (минимум 10 точки)
            int playerCost = 10;
            if (ratingValue > 0)
            {
                playerCost = (int)Math.Max(10, Math.Round(ratingValue * 5));
            }

            // 2. Check if user has enough points
            if (teamSettings.Points < playerCost)
            {
                 TempData["Error"] = $"Недостатъчно точки! Този играч струва {playerCost} точки, а вие имате само {teamSettings.Points}.";
                 return RedirectToAction("Index");
            }

            // Check if player exists in user's team
            var exists = _context.UserPlayers.Any(up => up.UserId == user.Id && up.PlayerApiId == playerId);
            if (exists)
            {
                TempData["Error"] = "Играчът вече е във вашия отбор!";
                return RedirectToAction("Index");
            }

            var userPlayer = new Models.UserPlayer
            {
                UserId = user.Id,
                PlayerApiId = playerId,
                PlayerName = playerName,
                Position = position,
                Rating = ratingValue,
                TeamName = teamName
            };

            // 3. Deduct points and Save
            teamSettings.Points -= playerCost;
            _context.UserTeamSettings.Update(teamSettings);

            _context.UserPlayers.Add(userPlayer);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Играчът е добавен успешно! (-{playerCost} точки)";
            return RedirectToAction("Index");
        }

        private async Task<PlayerApiResponse> FetchApiData(string url)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-apisports-key", ApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromSeconds(30);

            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            
            return JsonConvert.DeserializeObject<PlayerApiResponse>(json);
        }
    }

    // --- Models ---

    public class PlayerApiResponse
    {
        [JsonProperty("errors")]
        public object Errors { get; set; }

        [JsonProperty("paging")]
        public PagingInfo Paging { get; set; }

        [JsonProperty("response")]
        public List<PlayerEntry> Response { get; set; }
    }

    public class PagingInfo
    {
        [JsonProperty("current")]
        public int Current { get; set; }
        
        [JsonProperty("total")]
        public int Total { get; set; }
    }

    public class PlayerEntry
    {
        [JsonProperty("player")]
        public PlayerDetails Player { get; set; }

        [JsonProperty("statistics")]
        public List<PlayerStats> Statistics { get; set; }
    }

    public class PlayerDetails
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public int Age { get; set; }
        public string Nationality { get; set; }
        public string Photo { get; set; }
    }

    public class PlayerStats
    {
        [JsonProperty("team")]
        public TeamDetails Team { get; set; }

        [JsonProperty("league")]
        public LeagueDetails League { get; set; }

        [JsonProperty("games")]
        public Games Games { get; set; }

        [JsonProperty("goals")]
        public PlayerGoals Goals { get; set; }
    }

    public class TeamDetails
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("logo")]
        public string Logo { get; set; }
    }

    public class LeagueDetails
    {
        public string Name { get; set; }
        public string Country { get; set; }
        public string Logo { get; set; }
        public string Flag { get; set; }
        public int Season { get; set; }
    }

    public class Games
    {
        // Старото (грешно) изписване от API-то
        [JsonProperty("appearences")]
        public int? AppearencesLegacy { get; set; }

        // Правилното изписване (ако са го оправили)
        [JsonProperty("appearances")]
        public int? AppearencesCorrect { get; set; }

        // Интелигентно пропърти, което връща което има стойност
        public int? Appearences => AppearencesLegacy ?? AppearencesCorrect ?? 0;

        [JsonProperty("minutes")]
        public int? Minutes { get; set; }

        [JsonProperty("position")]
        public string Position { get; set; }

        [JsonProperty("rating")]
        public string Rating { get; set; }

        [JsonProperty("captain")]
        public bool Captain { get; set; }
    }

    public class PlayerGoals
    {
        [JsonProperty("total")]
        public int? Total { get; set; }

        [JsonProperty("assists")]
        public int? Assists { get; set; }
    }
}
