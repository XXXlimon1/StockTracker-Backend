using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTracker.API.Data;
using StockTracker.API.Services;

namespace StockTracker.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MarketsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly YahooFinanceService _yahooService;
        private readonly ILogger<MarketsController> _logger;

        private static readonly List<string> Bist30 = new()
        {
            "AKBNK", "ARCLK", "ASELS", "BIMAS", "DOHOL", "EKGYO", "EREGL",
            "FROTO", "GARAN", "HALKB", "ISCTR", "KCHOL", "KOZAL", "KRDMD",
            "MGROS", "PETKM", "PGSUS", "SAHOL", "SASA", "SISE", "TAVHL",
            "TCELL", "THYAO", "TKFEN", "TOASO", "TSKB", "TTKOM", "TUPRS",
            "VAKBN", "YKBNK"
        };

        private static readonly List<string> YildizPazar = new()
        {
            "THYAO", "GARAN", "AKBNK", "ISCTR", "SAHOL", "KCHOL", "FROTO",
            "TUPRS", "EREGL", "BIMAS", "TCELL", "ASELS", "SISE", "TOASO",
            "TKFEN", "KOZAL", "TAVHL", "PGSUS", "MGROS", "EKGYO"
        };

        private static readonly List<string> AnaPazar = new()
        {
            "AEFES", "ALARK", "ALGYO", "ALKIM", "ANACM", "ASUZU", "AYCES",
            "BAGFS", "BANVT", "BRISA", "BRYAT", "BUCIM", "CIMSA", "CLEBI",
            "DEVA", "DOAS", "EGEEN", "ENKAI", "FENER", "GESAN"
        };

        public MarketsController(AppDbContext context, YahooFinanceService yahooService, ILogger<MarketsController> logger)
        {
            _context = context;
            _yahooService = yahooService;
            _logger = logger;
        }

        [HttpGet("bist30")]
        public async Task<ActionResult> GetBist30() => await GetFromDb(Bist30);

        [HttpGet("yildiz")]
        public async Task<ActionResult> GetYildizPazar() => await GetFromDb(YildizPazar);

        [HttpGet("ana")]
        public async Task<ActionResult> GetAnaPazar() => await GetFromDb(AnaPazar);

        [HttpGet("search")]
        public async Task<ActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return BadRequest("En az 2 karakter girin");

            var ticker = q.ToUpper().Trim();
            var fullTicker = ticker.Contains(".IS") ? ticker : $"{ticker}.IS";
            var cleanTicker = fullTicker.Replace(".IS", "");

            var cached = await _context.StockPrices
                .FirstOrDefaultAsync(s => s.Ticker == fullTicker);

            decimal currentPrice = cached?.Price ?? 0m;
            decimal changePercent = 0m;

            if (cached != null)
            {
                changePercent = await CalcDailyChange(fullTicker, currentPrice);
            }
            else
            {
                var price = await _yahooService.GetPrice(fullTicker);
                if (!price.HasValue) return NotFound($"{ticker} bulunamadı");
                currentPrice = price.Value;
            }

            return Ok(new[] { new { ticker = cleanTicker, price = currentPrice, change = 0m, changePercent } });
        }

        private async Task<ActionResult> GetFromDb(List<string> tickers)
        {
            var fullTickers = tickers.Select(t => $"{t}.IS").ToList();

            var prices = await _context.StockPrices
                .Where(s => fullTickers.Contains(s.Ticker))
                .ToListAsync();

            // Dün UTC gece yarısı
            var yesterdayStart = DateTime.UtcNow.Date.AddDays(-1);
            var yesterdayEnd = DateTime.UtcNow.Date;

            // Dünkü son fiyatları çek
            var yesterdayPrices = await _context.StockPriceHistories
                .Where(h => fullTickers.Contains(h.Ticker) &&
                            h.RecordedAt >= yesterdayStart &&
                            h.RecordedAt < yesterdayEnd)
                .GroupBy(h => h.Ticker)
                .Select(g => new { Ticker = g.Key, Price = g.OrderByDescending(h => h.RecordedAt).First().Price })
                .ToListAsync();

            var result = tickers
                .Select(t =>
                {
                    var fullT = $"{t}.IS";
                    var price = prices.FirstOrDefault(p => p.Ticker == fullT);
                    var yesterday = yesterdayPrices.FirstOrDefault(p => p.Ticker == fullT);

                    decimal changePercent = 0m;
                    if (price != null && yesterday != null && yesterday.Price != 0)
                        changePercent = ((price.Price - yesterday.Price) / yesterday.Price) * 100;

                    return new
                    {
                        ticker = t,
                        price = price?.Price ?? 0m,
                        change = 0m,
                        changePercent
                    };
                })
                .Where(r => r.price > 0)
                .ToList();

            _logger.LogInformation($"Markets: Returning {result.Count}/{tickers.Count} stocks");
            return Ok(result);
        }

        private async Task<decimal> CalcDailyChange(string fullTicker, decimal currentPrice)
        {
            var yesterdayStart = DateTime.UtcNow.Date.AddDays(-1);
            var yesterdayEnd = DateTime.UtcNow.Date;

            var yesterdayPrice = await _context.StockPriceHistories
                .Where(h => h.Ticker == fullTicker &&
                            h.RecordedAt >= yesterdayStart &&
                            h.RecordedAt < yesterdayEnd)
                .OrderByDescending(h => h.RecordedAt)
                .Select(h => h.Price)
                .FirstOrDefaultAsync();

            if (yesterdayPrice == 0) return 0m;
            return ((currentPrice - yesterdayPrice) / yesterdayPrice) * 100;
        }
    }
}
