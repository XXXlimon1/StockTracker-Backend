using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTracker.API.Data;
using StockTracker.API.Models;
using System.Security.Claims;

namespace StockTracker.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WatchlistController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WatchlistController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var userId) ? userId : 0;
        }

        // GET: api/Watchlist
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Watchlist>>> GetWatchlist()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            return await _context.Watchlists
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();
        }

        // POST: api/Watchlist/{ticker}
        [HttpPost("{ticker}")]
        public async Task<IActionResult> AddToWatchlist(string ticker)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var fullTicker = ticker.Contains(".IS") ? ticker.ToUpper() : $"{ticker.ToUpper()}.IS";

            // Zaten varsa tekrar ekleme
            var exists = await _context.Watchlists
                .AnyAsync(w => w.UserId == userId && w.Ticker == fullTicker);

            if (exists) return Ok();

            _context.Watchlists.Add(new Watchlist
            {
                UserId = userId,
                Ticker = fullTicker,
                AddedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Watchlist/{ticker}
        [HttpDelete("{ticker}")]
        public async Task<IActionResult> RemoveFromWatchlist(string ticker)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var fullTicker = ticker.Contains(".IS") ? ticker.ToUpper() : $"{ticker.ToUpper()}.IS";

            var item = await _context.Watchlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.Ticker == fullTicker);

            if (item == null) return NotFound();

            _context.Watchlists.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/Watchlist/check/{ticker}
        [HttpGet("check/{ticker}")]
        public async Task<ActionResult<bool>> IsInWatchlist(string ticker)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var fullTicker = ticker.Contains(".IS") ? ticker.ToUpper() : $"{ticker.ToUpper()}.IS";

            var exists = await _context.Watchlists
                .AnyAsync(w => w.UserId == userId && w.Ticker == fullTicker);

            return Ok(exists);
        }
    }
}
