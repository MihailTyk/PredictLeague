using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictLeague.Data;

namespace PredictLeague.Controllers
{
    [Authorize] // трябва да е логнат
    public class MyPredictionsController : Controller
    {
        private readonly PredictLeagueContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MyPredictionsController(PredictLeagueContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userName = User.Identity.Name;

            var predictions = await _context.Prediction
                .Include(p => p.Match)
                .Where(p => p.UserName == userName)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(predictions);
        }
    }
}
