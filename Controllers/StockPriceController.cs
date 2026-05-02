using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTracker.API.Data;
using StockTracker.API.Models;
using StockTracker.API.Services;

namespace StockTracker.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StockPriceController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly YahooFinanceService _yahooService;

        public StockPriceController(AppDbContext context, YahooFinanceService yahooService)
        {
            _context = context;
            _yahooService = yahooService;
        }

        // GET: api/StockPrice - Tüm anlık fiyatlar (DB'den)
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

        // GET: api/StockPrice/{ticker} - Anlık fiyat (DB'den)
        [HttpGet("{ticker}")]
        public async Task<ActionResult> GetPrice(string ticker)
        {
            var fullTicker = ticker.Contains(".IS") ? ticker : ticker + ".IS";

            var stockPrice = await _context.StockPrices
                .FirstOrDefaultAsync(sp => sp.Ticker.ToUpper() == fullTicker.ToUpper());

            if (stockPrice == null)
                return NotFound($"No price data found for {ticker}.");

            return Ok(new
            {
                ticker = ticker.Replace(".IS", "").ToUpper(),
                price = stockPrice.Price,
                updatedAt = stockPrice.UpdatedAt
            });
        }

        // GET: api/StockPrice/{ticker}/history?days=7 - DB'deki geçmiş (background job kayıtları)
        [HttpGet("{ticker}/history")]
        public async Task<ActionResult> GetPriceHistory(string ticker, [FromQuery] int days = 7)
        {
            if (days > 30) days = 30;
            var fullTicker = ticker.Contains(".IS") ? ticker : ticker + ".IS";
            var cutoff = DateTime.UtcNow.AddDays(-days);

            var history = await _context.StockPriceHistories
                .Where(h => h.Ticker.ToUpper() == fullTicker.ToUpper() && h.RecordedAt >= cutoff)
                .OrderBy(h => h.RecordedAt)
                .Select(h => new { price = h.Price, recordedAt = h.RecordedAt })
                .ToListAsync();

            return Ok(new { ticker = ticker.Replace(".IS", "").ToUpper(), days, dataPoints = history.Count, history });
        }

        // GET: api/StockPrice/{ticker}/yahoo-history?range=1mo - Yahoo Finance'tan geçmiş
        // range: 5d, 1mo, 3mo, 6mo, 1y
        [HttpGet("{ticker}/yahoo-history")]
        public async Task<ActionResult> GetYahooHistory(string ticker, [FromQuery] string range = "1mo")
        {
            var validRanges = new[] { "5d", "1mo", "3mo", "6mo", "1y" };
            if (!validRanges.Contains(range)) range = "1mo";

            var data = await _yahooService.GetHistoricalPrices(ticker, range);

            if (!data.Any())
                return NotFound($"No historical data found for {ticker}");

            var history = data.Select(x => new
            {
                price = x.Close,
                recordedAt = x.Date
            }).ToList();

            return Ok(new
            {
                ticker = ticker.Replace(".IS", "").ToUpper(),
                range,
                dataPoints = history.Count,
                history
            });
        }
    }
}
