using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictLeague.Data;
using PredictLeague.Models;

namespace PredictLeague.Controllers
{
    [Authorize]
    public class MyTeamController : Controller
    {
        private readonly PredictLeagueContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MyTeamController(PredictLeagueContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return LocalRedirect("/Identity/Account/Login");

            // Setup Team Settings if not exists
            var teamSettings = await _context.UserTeamSettings
                .FirstOrDefaultAsync(ts => ts.UserId == user.Id);

            if (teamSettings == null)
            {
                teamSettings = new UserTeamSettings
                {
                    UserId = user.Id,
                    Formation = "4-4-2"
                };
                _context.UserTeamSettings.Add(teamSettings);
                await _context.SaveChangesAsync();
            }

            // Get Players
            var myPlayers = await _context.UserPlayers
                .Where(up => up.UserId == user.Id)
                .ToListAsync();

            var model = new MyTeamViewModel
            {
                Settings = teamSettings,
                AllPlayers = myPlayers,
                StartingLineup = myPlayers.Where(p => p.IsStarter).ToList(),
                Bench = myPlayers.Where(p => !p.IsStarter).ToList()
            };

            ViewBag.Positions = GetFormationPositions(teamSettings.Formation);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateFormation(string formation)
        {
            var user = await _userManager.GetUserAsync(User);
            var settings = await _context.UserTeamSettings.FirstOrDefaultAsync(ts => ts.UserId == user.Id);
            
            if (settings != null)
            {
                settings.Formation = formation;
                // Optional: Reset positions if they don't fit? Or try to adapt?
                // For now, let's keep them, but user might see weird slots.
                // Better approach: Clear positions that don't exist in new formation?
                // Or simply let the UI handle empty slots.
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AssignPlayer(int playerId, string position)
        {
            var user = await _userManager.GetUserAsync(User);
            var player = await _context.UserPlayers.FirstOrDefaultAsync(p => p.Id == playerId && p.UserId == user.Id);
            
            if (player == null) return NotFound();

            // 1. If another player is already in this position, move them to bench
            var existing = await _context.UserPlayers.FirstOrDefaultAsync(p => p.UserId == user.Id && p.FieldPosition == position);
            if (existing != null)
            {
                existing.FieldPosition = null;
            }

            // 2. If this player was elsewhere, that old spot becomes free (handled automatically)
            
            // 3. Assign
            player.FieldPosition = position;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemovePlayer(int playerId)
        {
             var user = await _userManager.GetUserAsync(User);
             var player = await _context.UserPlayers.FirstOrDefaultAsync(p => p.Id == playerId && p.UserId == user.Id);
             
             if (player != null)
             {
                 player.FieldPosition = null;
                 await _context.SaveChangesAsync();
             }
             return RedirectToAction("Index");
        }

        private List<string> GetFormationPositions(string formation)
        {
            // Define positions based on formation
            var positions = new List<string> { "GK" };
            
            switch (formation)
            {
                case "4-3-3":
                    positions.AddRange(new[] { "LB", "CB1", "CB2", "RB", "CM1", "CM2", "CM3", "LW", "ST", "RW" });
                    break;
                case "4-4-2":
                    positions.AddRange(new[] { "LB", "CB1", "CB2", "RB", "LM", "CM1", "CM2", "RM", "ST1", "ST2" });
                    break;
                case "3-5-2":
                     positions.AddRange(new[] { "CB1", "CB2", "CB3", "LM", "CM1", "CM2", "CM3", "RM", "ST1", "ST2" });
                    break;
                 case "5-3-2":
                     positions.AddRange(new[] { "LB", "CB1", "CB2", "CB3", "RB", "CM1", "CM2", "CM3", "ST1", "ST2" });
                    break;
                default: // 4-4-2 default
                     positions.AddRange(new[] { "LB", "CB1", "CB2", "RB", "LM", "CM1", "CM2", "RM", "ST1", "ST2" });
                    break;
            }
            return positions;
        }
    }

    public class MyTeamViewModel
    {
        public UserTeamSettings Settings { get; set; }
        public List<UserPlayer> AllPlayers { get; set; }
        public List<UserPlayer> StartingLineup { get; set; }
        public List<UserPlayer> Bench { get; set; }
    }
}
