using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace PredictLeague.Controllers
{
    public class FootballTransferService
    {
        private readonly HttpClient _http;
        private readonly ILogger<FootballTransferService> _logger;
        private readonly IMemoryCache _cache;
        private readonly string _apiKey;

        public FootballTransferService(HttpClient http, ILogger<FootballTransferService> logger, IConfiguration configuration, IMemoryCache cache)
        {
            _http = http;
            _logger = logger;
            _cache = cache;
            _apiKey = configuration["ApiKeys:ApiSports"] ?? "";
        }

        public async Task<List<TransferEntry>> GetRecentTransfersAsync()
        {
            string cacheKey = "RecentEuropeanTransfers";
            if (_cache.TryGetValue(cacheKey, out List<TransferEntry> cachedTransfers))
            {
                return cachedTransfers;
            }

            try
            {
                if (string.IsNullOrEmpty(_apiKey)) return new List<TransferEntry>();

                // 🌍 Елитни отбори от Европа
                int[] teamIds = { 33, 40, 42, 50, 541, 529, 530, 157, 165, 168, 497, 496, 489, 85 };
                
                var allTransfers = new List<TransferEntry>();

                foreach (var teamId in teamIds)
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"https://v3.football.api-sports.io/transfers?team={teamId}");
                    request.Headers.Add("x-apisports-key", _apiKey);
                    
                    var response = await _http.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<TransferApiResponse>(json);
                        if (result?.Response != null)
                        {
                            allTransfers.AddRange(result.Response);
                        }
                    }
                    await Task.Delay(50);
                }

                var finalTransfers = allTransfers
                    .Where(t => t.Transfers != null && t.Transfers.Any())
                    .Select(t => new { 
                        Player = t.Player, 
                        RecentTransfers = t.Transfers
                            .Where(tr => DateTime.TryParse(tr.Date, out var dt) && dt.Year >= 2025)
                            .OrderByDescending(tr => tr.Date)
                            .ToList() 
                    })
                    .Where(t => t.RecentTransfers.Any())
                    .OrderByDescending(t => DateTime.Parse(t.RecentTransfers.First().Date))
                    .Take(10)
                    .Select(t => new TransferEntry { 
                        Player = t.Player, 
                        Transfers = t.RecentTransfers 
                    })
                    .ToList();

                _cache.Set(cacheKey, finalTransfers, TimeSpan.FromHours(3));

                return finalTransfers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while getting transfers");
                return new List<TransferEntry>();
            }
        }
    }

    public class TransferApiResponse
    {
        [JsonProperty("response")]
        public List<TransferEntry> Response { get; set; }
    }

    public class TransferEntry
    {
        [JsonProperty("player")]
        public PlayerShort Player { get; set; }
        [JsonProperty("transfers")]
        public List<TransferDetail> Transfers { get; set; }
    }

    public class PlayerShort
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class TransferDetail
    {
        [JsonProperty("date")]
        public string Date { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("teams")]
        public TeamsOutIn Teams { get; set; }
    }

    public class TeamsOutIn
    {
        [JsonProperty("out")]
        public TeamShort Out { get; set; }
        [JsonProperty("in")]
        public TeamShort In { get; set; }
    }

    public class TeamShort
    {
        [JsonProperty("id")]
        public int? Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("logo")]
        public string Logo { get; set; }
    }
}
