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

        public MarketsController(YahooFinanceService yahooService)
        {
            _yahooService = yahooService;
        }

        // BIST 30 hisseleri
        private static readonly List<string> Bist30 = new()
        {
            "AKBNK", "ARCLK", "ASELS", "BIMAS", "DOHOL", "EKGYO", "EREGL",
            "FROTO", "GARAN", "HALKB", "ISCTR", "KCHOL", "KOZAL", "KRDMD",
            "MGROS", "PETKM", "PGSUS", "SAHOL", "SASA", "SISE", "TAVHL",
            "TCELL", "THYAO", "TKFEN", "TOASO", "TSKB", "TTKOM", "TUPRS",
            "VAKBN", "YKBNK"
        };

        // Yıldız Pazar - büyük şirketler
        private static readonly List<string> YildizPazar = new()
        {
            "THYAO", "GARAN", "AKBNK", "ISCTR", "SAHOL", "KCHOL", "FROTO",
            "TUPRS", "EREGL", "BIMAS", "TCELL", "ASELS", "SISE", "TOASO",
            "TKFEN", "KOZAL", "TAVHL", "PGSUS", "MGROS", "EKGYO"
        };

        // Ana Pazar - orta büyüklükteki şirketler
        private static readonly List<string> AnaPazar = new()
        {
            "AEFES", "ALARK", "ALGYO", "ALKIM", "ANACM", "ASUZU", "AYCES",
            "BAGFS", "BANVT", "BRISA", "BRYAT", "BUCIM", "CIMSA", "CLEBI",
            "DEVA", "DOAS", "EGEEN", "ENKAI", "FENER", "GESAN"
        };

        // GET: api/Markets/bist30
        [HttpGet("bist30")]
        public async Task<ActionResult> GetBist30()
        {
            return await GetPrices(Bist30);
        }

        // GET: api/Markets/yildiz
        [HttpGet("yildiz")]
        public async Task<ActionResult> GetYildizPazar()
        {
            return await GetPrices(YildizPazar);
        }

        // GET: api/Markets/ana
        [HttpGet("ana")]
        public async Task<ActionResult> GetAnaPazar()
        {
            return await GetPrices(AnaPazar);
        }

        // GET: api/Markets/search?q=THY
        [HttpGet("search")]
        public async Task<ActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return BadRequest("En az 2 karakter girin");

            var ticker = q.ToUpper().Trim();
            var fullTicker = ticker.Contains(".IS") ? ticker : $"{ticker}.IS";

            var price = await _yahooService.GetPrice(fullTicker);
            if (!price.HasValue)
                return NotFound($"{ticker} bulunamadı");

            return Ok(new[]
            {
                new { ticker, price = price.Value, change = 0m, changePercent = 0m }
            });
        }

        private async Task<ActionResult> GetPrices(List<string> tickers)
        {
            var result = new List<object>();

            // Paralel değil, sıralı — Yahoo rate limit'e takılmayalım
            foreach (var ticker in tickers)
            {
                try
                {
                    var fullTicker = $"{ticker}.IS";
                    var price = await _yahooService.GetPrice(fullTicker);
                    if (price.HasValue)
                    {
                        result.Add(new
                        {
                            ticker,
                            price = price.Value,
                            change = 0m,
                            changePercent = 0m
                        });
                    }
                }
                catch { }

                await Task.Delay(300); // Rate limit
            }

            return Ok(result);
        }
    }
}
