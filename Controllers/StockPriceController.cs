using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTracker.API.Data;

namespace StockTracker.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StockPriceController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StockPriceController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/StockPrice/{ticker} — Anlık fiyat (DB'den, background job güncelliyor)
        [HttpGet("{ticker}")]
        public async Task<ActionResult> GetPrice(string ticker)
        {
            var fullTicker = ticker.Contains(".IS") ? ticker : ticker + ".IS";

            var stockPrice = await _context.StockPrices
                .FirstOrDefaultAsync(sp => sp.Ticker.ToUpper() == fullTicker.ToUpper());

            if (stockPrice == null)
                return NotFound($"No price data found for {ticker}. Background job may not have run yet.");

            return Ok(new
            {
                ticker = ticker.Replace(".IS", "").ToUpper(),
                price = stockPrice.Price,
                updatedAt = stockPrice.UpdatedAt
            });
        }

        // GET: api/StockPrice — Tüm takip edilen hisseler
        [HttpGet]
        public async Task<ActionResult> GetAllPrices()
        {
            var prices = await _context.StockPrices
                .OrderBy(sp => sp.Ticker)
                .Select(sp => new
                {
                    ticker = sp.Ticker.Replace(".IS", "").ToUpper(),
                    price = sp.Price,
                    updatedAt = sp.UpdatedAt
                })
                .ToListAsync();

            return Ok(prices);
        }

        // GET: api/StockPrice/{ticker}/history?days=7 — Fiyat geçmişi (grafik için)
        [HttpGet("{ticker}/history")]
        public async Task<ActionResult> GetPriceHistory(string ticker, [FromQuery] int days = 7)
        {
            if (days > 30) days = 30; // Max 30 gün

            var fullTicker = ticker.Contains(".IS") ? ticker : ticker + ".IS";
            var cutoff = DateTime.UtcNow.AddDays(-days);

            var history = await _context.StockPriceHistories
                .Where(h => h.Ticker.ToUpper() == fullTicker.ToUpper() && h.RecordedAt >= cutoff)
                .OrderBy(h => h.RecordedAt)
                .Select(h => new
                {
                    price = h.Price,
                    recordedAt = h.RecordedAt
                })
                .ToListAsync();

            return Ok(new
            {
                ticker = ticker.Replace(".IS", "").ToUpper(),
                days,
                dataPoints = history.Count,
                history
            });
        }
    }
}
