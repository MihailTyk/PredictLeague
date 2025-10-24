using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PredictLeague.Data;
using PredictLeague.Models;

namespace PredictLeague.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class PredictionsApiController : ControllerBase
    {
        private readonly PredictLeagueContext _context;

        public PredictionsApiController(PredictLeagueContext context)
        {
            _context = context;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<Prediction>>> GetPredictions()
        {
            return await _context.Prediction
                .Include(p => p.Match)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        
        [HttpGet("{matchId}")]
        public async Task<ActionResult<IEnumerable<Prediction>>> GetPredictionsForMatch(int matchId)
        {
            var predictions = await _context.Prediction
                .Where(p => p.MatchId == matchId)
                .Include(p => p.Match)
                .ToListAsync();

            if (!predictions.Any())
                return NotFound(new { message = "No predictions found for this match." });

            return predictions;
        }

        
        [HttpPost]
        public async Task<ActionResult> PostPrediction([FromBody] Prediction prediction)
        {
            if (prediction == null)
                return BadRequest(new { message = "Prediction data is missing." });

            var match = await _context.Match.FindAsync(prediction.MatchId);
            if (match == null)
                return NotFound(new { message = "Match not found." });

            prediction.CreatedAt = DateTime.Now;
            _context.Prediction.Add(prediction);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Prediction saved successfully!" });
        }
    }
}
