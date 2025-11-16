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
            var news = await _newsService.GetNewsAsync();
            return View(news);  // <-- изпращаме новините към View
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
