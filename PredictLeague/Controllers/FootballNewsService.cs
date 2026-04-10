using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
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
                if (string.IsNullOrEmpty(apiKey)) return new List<FootballNews>();

                // Най-простата възможна заявка
                string url = $"https://newsdata.io/api/1/news?apikey={apiKey}&q=football&language=en";
                
                var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode) return GetFallbackNews();

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<NewsDataIoResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (data?.results == null || !data.results.Any()) return GetFallbackNews();

                // Връщаме базовите филтри за по-чисто съдържание
                return data.results
                    .Where(a => 
                        !((a.title ?? "") + (a.description ?? "")).Contains("NFL", StringComparison.OrdinalIgnoreCase) && 
                        !((a.title ?? "") + (a.description ?? "")).Contains("NBA", StringComparison.OrdinalIgnoreCase) &&
                        !((a.title ?? "") + (a.description ?? "")).Contains("Cricket", StringComparison.OrdinalIgnoreCase) &&
                        !((a.title ?? "") + (a.description ?? "")).Contains("MLB", StringComparison.OrdinalIgnoreCase) &&
                        !((a.title ?? "") + (a.description ?? "")).Contains("Xbox", StringComparison.OrdinalIgnoreCase))
                    .Select(article => new FootballNews
                    {
                        title = article.title ?? "",
                        description = article.description ?? "",
                        url = article.link ?? "",
                        urlToImage = article.image_url ?? "",
                        publishedAt = article.pubDate ?? ""
                    })
                    .GroupBy(n => n.title)
                    .Select(g => g.First())
                    .ToList();
            }
            catch
            {
                return GetFallbackNews();
            }
        }

        private List<FootballNews> GetFallbackNews()
        {
            // Резервни новини, ако API-то откаже, за да не е празен екрана
            return new List<FootballNews>
            {
                new FootballNews {
                    title = "Manchester United планира мащабна лятна селекция",
                    description = "Ръководството на клуба подготвя сериозни трансфери за новия сезон, за да върне отбора на върха в Премиър лийг.",
                    url = "https://www.manutd.com",
                    urlToImage = "https://images.unsplash.com/photo-1574629810360-7efbbe195018?auto=format&fit=crop&w=800&q=80",
                    publishedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
        }
    }

    public class NewsDataIoResponse
    {
        [JsonPropertyName("status")]
        public string status { get; set; }
        
        [JsonPropertyName("totalResults")]
        public int totalResults { get; set; }
        
        [JsonPropertyName("results")]
        public List<NewsDataIoArticle> results { get; set; }
    }

    public class NewsDataIoArticle
    {
        [JsonPropertyName("article_id")]
        public string article_id { get; set; }
        
        [JsonPropertyName("title")]
        public string title { get; set; }
        
        [JsonPropertyName("link")]
        public string link { get; set; }
        
        [JsonPropertyName("description")]
        public string description { get; set; }
        
        [JsonPropertyName("image_url")]
        public string image_url { get; set; }
        
        [JsonPropertyName("pubDate")]
        public string pubDate { get; set; }
    }

    public class FootballNews
    {
        public string title { get; set; }
        public string description { get; set; }
        public string url { get; set; }
        public string urlToImage { get; set; }
        public string publishedAt { get; set; }
    }
}
