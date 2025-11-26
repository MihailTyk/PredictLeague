using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;

namespace PredictLeague.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FootballApiController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public FootballApiController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
        }

        
        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingMatches()
        {
            try
            {
                string apiKey = _configuration["ApiKeys:ApiSports"] ?? "";

               
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
