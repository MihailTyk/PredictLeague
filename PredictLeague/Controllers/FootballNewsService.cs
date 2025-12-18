using System.Net.Http;
using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PredictLeague.Controllers
{
    public class FootballNewsService
    {
        private readonly HttpClient _http;
        private readonly ILogger<FootballNewsService> _logger;
        private readonly string apiKey;

        public FootballNewsService(HttpClient http, ILogger<FootballNewsService> logger, IConfiguration configuration)
        {
            _http = http;
            _logger = logger;
            apiKey = configuration["ApiKeys:NewsDataIo"] ?? "";
        }

        public async Task<List<FootballNews>> GetNewsAsync()
        {
            try
            {
                // Ако API ключът не е настроен, връщаме празен списък
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("NewsData.io API key is not configured");
                    return new List<FootballNews>();
                }

                // NewsData.io endpoint - използваме latest news endpoint
                // NewsData.io endpoint - използваме по-специфични ключови думи за да избегнем американски футбол
                string url = $"https://newsdata.io/api/1/news?apikey={apiKey}&category=sports&q=soccer OR \"premier league\" OR \"champions league\"&language=en";
                
                _logger.LogInformation($"Making request to NewsData.io: {url.Replace(apiKey, "***")}");

                var response = await _http.GetAsync(url);

                _logger.LogInformation($"Response status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"NewsData.io request failed. Status: {response.StatusCode}, Response: {errorContent}");
                    return new List<FootballNews>();
                }

                var json = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Response received. Length: {json.Length} characters");
                _logger.LogInformation($"Response preview (first 500 chars): {json.Substring(0, Math.Min(500, json.Length))}");
                
                var data = JsonSerializer.Deserialize<NewsDataIoResponse>(json);

                if (data?.results == null)
                {
                    _logger.LogWarning("NewsData.io returned null or empty results");
                    return new List<FootballNews>();
                }

                _logger.LogInformation($"Successfully loaded {data.results.Count} news articles from NewsData.io");
                
                // Конвертираме NewsData.io формат към нашия формат
                var footballNews = data.results.Select(article => new FootballNews
                {
                    title = article.title ?? "",
                    description = article.description ?? "",
                    url = article.link ?? "",
                    urlToImage = article.image_url ?? ""
                }).ToList();

                return footballNews;
            }
            catch (Exception ex)
            {
                // При грешка връщаме празен списък
                _logger.LogError(ex, "Error loading football news from NewsData.io. Exception: {Exception}", ex);
                return new List<FootballNews>();
            }
        }
    }

    // NewsData.io response format
    public class NewsDataIoResponse
    {
        public string status { get; set; }
        public int totalResults { get; set; }
        public List<NewsDataIoArticle> results { get; set; }
    }

    public class NewsDataIoArticle
    {
        public string article_id { get; set; }
        public string title { get; set; }
        public string link { get; set; }
        public string description { get; set; }
        public string image_url { get; set; }
        public string pubDate { get; set; }
    }

    public class FootballNews
    {
        public string title { get; set; }
        public string description { get; set; }
        public string url { get; set; }
        public string urlToImage { get; set; }
    }
}
