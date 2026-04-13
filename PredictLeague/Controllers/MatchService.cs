using Microsoft.EntityFrameworkCore;
using PredictLeague.Data;
using PredictLeague.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace PredictLeague.Controllers
{
    public class MatchService
    {
        private readonly PredictLeagueContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MatchService> _logger;
        private readonly HttpClient _httpClient;

        public MatchService(PredictLeagueContext context, IConfiguration configuration, ILogger<MatchService> logger, HttpClient httpClient)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<int> RefreshFinishedMatchesAsync()
        {
            // Намираме мачове, които не са завършени, започнали са преди поне 2 часа и имат FixtureId
            var matchesToSync = await _context.Match
                .Where(m => !m.IsFinished && m.FixtureId != null && m.StartTime < DateTime.Now.AddHours(-1.5))
                .ToListAsync();

            if (!matchesToSync.Any()) return 0;

            string apiKey = _configuration["ApiKeys:ApiSports"] ?? "";
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-apisports-key", apiKey);

            int updatedCount = 0;

            foreach (var match in matchesToSync)
            {
                try
                {
                    _logger.LogInformation($"Syncing match {match.Id} (Fixture {match.FixtureId})");
                    var response = await _httpClient.GetAsync($"https://v3.football.api-sports.io/fixtures?id={match.FixtureId}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<FixtureDetailApiResponse>(json);
                        var detail = result?.response?.FirstOrDefault();

                        if (detail?.fixture?.status != null)
                        {
                            string status = detail.fixture.status.short_;
                            if (status == "FT" || status == "AET" || status == "PEN")
                            {
                                match.IsFinished = true;
                                match.HomeScore = detail.goals.home;
                                match.AwayScore = detail.goals.away;
                                
                                // Проверяваме за дузпа
                                if (detail.events != null)
                                {
                                    match.HadPenalty = detail.events.Any(e => 
                                        !string.IsNullOrEmpty(e.detail) && 
                                        e.detail.Contains("Penalty", StringComparison.OrdinalIgnoreCase));
                                }
                                
                                _context.Update(match);

                                // Обновяваме точките за всички предсказания
                                var predictions = await _context.Prediction.Where(p => p.MatchId == match.Id).ToListAsync();
                                foreach (var prediction in predictions)
                                {
                                    UpdatePredictionPoints(prediction, match);
                                }
                                updatedCount++;
                            }
                        }
                    }
                    // Малко изчакване за избягване на Rate Limit ако са много
                    await Task.Delay(100); 
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error syncing match {match.Id}");
                }
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return updatedCount;
        }

        public void UpdatePredictionPoints(Prediction prediction, Match match)
        {
            int oldPoints = prediction.Points;
            int newPoints = 0;

            if (prediction.PredictedHomeScore == match.HomeScore &&
                prediction.PredictedAwayScore == match.AwayScore)
            {
                newPoints = 10;
            }
            else 
            {
                if (prediction.PredictedHomeScore == match.HomeScore) newPoints += 3;
                if (prediction.PredictedAwayScore == match.AwayScore) newPoints += 3;

                if (newPoints == 0)
                {
                    bool outcomeMatches = (match.HomeScore > match.AwayScore && prediction.PredictedHomeScore > prediction.PredictedAwayScore) ||
                                         (match.HomeScore < match.AwayScore && prediction.PredictedHomeScore < prediction.PredictedAwayScore) ||
                                         (match.HomeScore == match.AwayScore && prediction.PredictedHomeScore == prediction.PredictedAwayScore);
                    if (outcomeMatches) newPoints = 1;
                }
            }

            // Бонус точки за позната дузпа (добавени върху основния резултат)
            if (match.HadPenalty.HasValue && prediction.PredictedPenalty.HasValue)
            {
                if (prediction.PredictedPenalty.Value == match.HadPenalty.Value)
                {
                    newPoints += 3; // +3 точки ако са познали за дузпа!
                }
            }

            if (newPoints != oldPoints)
            {
                var userSettings = _context.UserTeamSettings.FirstOrDefault(s => s.UserId == prediction.UserId);
                if (userSettings == null)
                {
                    // Създаваме настройките, ако не съществуват, за да не се губят точките
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
    }
}
