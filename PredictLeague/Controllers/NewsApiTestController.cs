using Microsoft.AspNetCore.Mvc;
using PredictLeague.Controllers;

namespace PredictLeague.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsApiTestController : ControllerBase
    {
        private readonly FootballNewsService _newsService;
        private readonly ILogger<NewsApiTestController> _logger;

        public NewsApiTestController(FootballNewsService newsService, ILogger<NewsApiTestController> logger)
        {
            _newsService = newsService;
            _logger = logger;
        }

        [HttpGet("test")]
        public async Task<IActionResult> TestNewsApi()
        {
            try
            {
                _logger.LogInformation("Testing NewsAPI...");
                
                var news = await _newsService.GetNewsAsync();
                
                return Ok(new
                {
                    success = true,
                    count = news.Count,
                    articles = news.Take(5), // Показваме само първите 5 за тест
                    message = news.Count > 0 
                        ? $"✅ NewsAPI работи! Заредени са {news.Count} новини." 
                        : "⚠️ NewsAPI върна празен списък. Провери API ключа и логовете."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing NewsAPI");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    message = "❌ Грешка при тестване на NewsAPI. Провери логовете за повече детайли."
                });
            }
        }

        [HttpGet("raw")]
        public async Task<IActionResult> TestNewsApiRaw()
        {
            try
            {
                _logger.LogInformation("Testing NewsAPI (raw response)...");
                
                var news = await _newsService.GetNewsAsync();
                
                return Ok(new
                {
                    success = true,
                    count = news.Count,
                    articles = news,
                    message = news.Count > 0 
                        ? $"✅ NewsAPI работи! Заредени са {news.Count} новини." 
                        : "⚠️ NewsAPI върна празен списък."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing NewsAPI (raw)");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}

