using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictLeague.Data;

namespace PredictLeague.Controllers
{
    [Authorize] 
    public class MyPredictionsController : Controller
    {
        private readonly PredictLeagueContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MyPredictionsController> _logger;

        public MyPredictionsController(PredictLeagueContext context, UserManager<IdentityUser> userManager, IConfiguration configuration, ILogger<MyPredictionsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var predictions = await _context.Prediction
                .Include(p => p.Match)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(predictions);
        }

    }
}
