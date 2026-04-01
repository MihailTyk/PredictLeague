using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictLeague.Data;
using PredictLeague.Models;

namespace PredictLeague.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly PredictLeagueContext _context;

        public AdminController(PredictLeagueContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            var predictions = await _context.Prediction.ToListAsync();

            // Изчисляваме кой колко прогнози има и колко са му total точките от тях
            var viewModel = users.Select(u => new AdminUserViewModel
            {
                UserId = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                TotalPredictions = predictions.Count(p => p.UserId == u.Id),
                TotalPoints = predictions.Where(p => p.UserId == u.Id).Sum(p => p.Points)
            })
            .OrderByDescending(x => x.TotalPoints)
            .ToList();

            return View(viewModel);
        }

        public async Task<IActionResult> UserPredictions(string id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) 
            {
                return NotFound();
            }

            var userPredictions = await _context.Prediction
                .Include(p => p.Match)
                .Where(p => p.UserId == id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.UserName = user.UserName;
            ViewBag.TotalPoints = userPredictions.Sum(p => p.Points);

            return View(userPredictions);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Охраняваме главния админ
            if (user.Email != null && user.Email.Equals("admin@predictleague.com", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Не можете да изтриете главния администратор!";
                return RedirectToAction(nameof(Index));
            }

            // Изтриваме свързаните записи, за да избегнем грешки с Foreign Keys
            var predictions = _context.Prediction.Where(p => p.UserId == id);
            _context.Prediction.RemoveRange(predictions);

            var players = _context.UserPlayers.Where(p => p.UserId == id);
            _context.UserPlayers.RemoveRange(players);

            var settings = _context.UserTeamSettings.Where(s => s.UserId == id);
            _context.UserTeamSettings.RemoveRange(settings);

            // Изтриваме самия потребител
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Потребителят {user.UserName} беше успешно изтрит.";
            return RedirectToAction(nameof(Index));
        }
    }
}
