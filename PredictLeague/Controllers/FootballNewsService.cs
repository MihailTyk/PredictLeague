using System.Net.Http;
using System.Text.Json;

namespace PredictLeague.Controllers
{
    public class FootballNewsService
    {
        private readonly HttpClient _http;
        private readonly string apiKey = "YOUR_API_KEY_HERE"; // <-- замени с твоя ключ

        public FootballNewsService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<FootballNews>> GetNewsAsync()
        {
            string url = $"https://newsapi.org/v2/top-headlines?category=sports&q=football&language=en&apiKey={apiKey}";

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new List<FootballNews>();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<NewsApiResponse>(json);

            return data?.articles ?? new List<FootballNews>();
        }
    }

    public class NewsApiResponse
    {
        public List<FootballNews> articles { get; set; }
    }

    public class FootballNews
    {
        public string title { get; set; }
        public string description { get; set; }
        public string url { get; set; }
        public string urlToImage { get; set; }
    }
}
