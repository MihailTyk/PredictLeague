using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PredictLeague.Models;

namespace PredictLeague.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly FootballNewsService _newsService;
        private readonly FootballTransferService _transferService;

        public HomeController(ILogger<HomeController> logger, FootballNewsService newsService, FootballTransferService transferService)
        {
            _logger = logger;
            _newsService = newsService;
            _transferService = transferService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var newsList = await _newsService.GetNewsAsync();
                ViewData["NewsList"] = newsList;

                var transfers = await _transferService.GetRecentTransfersAsync();
                ViewData["Transfers"] = transfers;
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
