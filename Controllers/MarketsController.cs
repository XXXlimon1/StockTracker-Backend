using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockTracker.API.Services;

namespace StockTracker.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MarketsController : ControllerBase
    {
        private readonly YahooFinanceService _yahooService;
        private readonly ILogger<MarketsController> _logger;

        // In-memory cache - 5 dakika
        private static Dictionary<string, (List<MarketStockDto> data, DateTime expiry)> _cache = new();
        private static readonly SemaphoreSlim _cacheLock = new(1, 1);

        public MarketsController(YahooFinanceService yahooService, ILogger<MarketsController> logger)
        {
            _yahooService = yahooService;
            _logger = logger;
        }

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

        [HttpGet("bist30")]
        public async Task<ActionResult> GetBist30() => await GetPricesCached("bist30", Bist30);

        [HttpGet("yildiz")]
        public async Task<ActionResult> GetYildizPazar() => await GetPricesCached("yildiz", YildizPazar);

        [HttpGet("ana")]
        public async Task<ActionResult> GetAnaPazar() => await GetPricesCached("ana", AnaPazar);

        [HttpGet("search")]
        public async Task<ActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return BadRequest("En az 2 karakter girin");

            var ticker = q.ToUpper().Trim();
            var fullTicker = ticker.Contains(".IS") ? ticker : $"{ticker}.IS";

            var data = await _yahooService.GetPriceWithChange(fullTicker);
            if (!data.HasValue)
                return NotFound($"{ticker} bulunamadı");

            var cleanTicker = fullTicker.Replace(".IS", "");
            return Ok(new[] { new MarketStockDto(cleanTicker, data.Value.Price, data.Value.Change, data.Value.ChangePercent) });
        }

        private async Task<ActionResult> GetPricesCached(string key, List<string> tickers)
        {
            // Cache kontrolü
            if (_cache.TryGetValue(key, out var cached) && cached.expiry > DateTime.UtcNow)
            {
                _logger.LogInformation($"Cache hit: {key}");
                return Ok(cached.data);
            }

            await _cacheLock.WaitAsync();
            try
            {
                // Double-check
                if (_cache.TryGetValue(key, out cached) && cached.expiry > DateTime.UtcNow)
                    return Ok(cached.data);

                var result = await FetchParallel(tickers);
                _cache[key] = (result, DateTime.UtcNow.AddMinutes(5));
                return Ok(result);
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private async Task<List<MarketStockDto>> FetchParallel(List<string> tickers)
        {
            // Batch'ler halinde paralel çek (5'li gruplar)
            var result = new System.Collections.Concurrent.ConcurrentBag<MarketStockDto>();
            var batches = tickers.Chunk(5);

            foreach (var batch in batches)
            {
                var tasks = batch.Select(async ticker =>
                {
                    try
                    {
                        var price = await _yahooService.GetPrice($"{ticker}.IS");
                        if (price.HasValue)
                            result.Add(new MarketStockDto(ticker, price.Value, 0m, 0m));
                    }
                    catch { }
                });

                await Task.WhenAll(tasks);
                await Task.Delay(200); // Batch'ler arası bekleme
            }

            // Orijinal sıralamayı koru
            return tickers
                .Select(t => result.FirstOrDefault(r => r.Ticker == t))
                .Where(r => r != null)
                .ToList()!;
        }
    }

    public record MarketStockDto(string Ticker, decimal Price, decimal Change, decimal ChangePercent);
}
