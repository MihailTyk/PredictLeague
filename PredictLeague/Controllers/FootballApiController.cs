using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace PredictLeague.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FootballApiController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public FootballApiController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        
        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingMatches()
        {
            try
            {
              
                string apiKey = "a1c5c63f7d7b71136b4512647b1da851";

               
                string url = "https://v3.football.api-sports.io/fixtures?league=39&season=2022";

               
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-apisports-key", apiKey);

               
                var response = await _httpClient.GetAsync(url);

               
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode,
                        $"⚠️ Грешка при извличане на данни от външното API. Код: {response.StatusCode}");
                }

               
                var json = await response.Content.ReadAsStringAsync();

              
                return Content(json, "application/json");
            }
            catch (System.Exception ex)
            {
                
                return StatusCode(500, $"❌ Грешка: {ex.Message}");
            }
        }
    }
}
