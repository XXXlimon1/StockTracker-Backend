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

            // Önce DB'den bak
            var cached = await _context.StockPrices
                .FirstOrDefaultAsync(s => s.Ticker == fullTicker);

            if (cached != null)
                return Ok(new[] { new { ticker = cleanTicker, price = cached.Price, change = 0m, changePercent = 0m } });

            // DB'de yoksa Yahoo'dan çek
            var price = await _yahooService.GetPrice(fullTicker);
            if (!price.HasValue)
                return NotFound($"{ticker} bulunamadı");

            return Ok(new[] { new { ticker = cleanTicker, price = price.Value, change = 0m, changePercent = 0m } });
        }

        private async Task<ActionResult> GetFromDb(List<string> tickers)
        {
            var fullTickers = tickers.Select(t => $"{t}.IS").ToList();

            // DB'den mevcut fiyatları çek
            var prices = await _context.StockPrices
                .Where(s => fullTickers.Contains(s.Ticker))
                .ToListAsync();

            var result = tickers
                .Select(t =>
                {
                    var price = prices.FirstOrDefault(p => p.Ticker == $"{t}.IS");
                    return new
                    {
                        ticker = t,
                        price = price?.Price ?? 0m,
                        change = 0m,
                        changePercent = price?.ChangePercent ?? 0m
                    };
                })
                .Where(r => r.price > 0)
                .ToList();

            _logger.LogInformation($"Markets: Returning {result.Count}/{tickers.Count} from DB cache");

            return Ok(result);
        }
    }
}
