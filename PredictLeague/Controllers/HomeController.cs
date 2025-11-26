using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PredictLeague.Models;

namespace PredictLeague.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly FootballNewsService _newsService;

        public HomeController(ILogger<HomeController> logger, FootballNewsService newsService)
        {
            _logger = logger;
            _newsService = newsService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var newsList = await _newsService.GetNewsAsync();
                // Използваме ViewData за да избегнем проблеми с динамични обекти
                ViewData["NewsList"] = newsList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading football news");
                ViewData["NewsList"] = new List<Controllers.FootballNews>();
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
