using Microsoft.AspNetCore.Mvc;
using StockTracker.API.Services;
using Microsoft.EntityFrameworkCore;
using StockTracker.API.Data;

namespace StockTracker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockPriceController : ControllerBase
    {
        private readonly StockPriceService _stockPriceService;
        private readonly AppDbContext _context;

        public StockPriceController(StockPriceService stockPriceService, AppDbContext context)
        {
            _stockPriceService = stockPriceService;
            _context = context;
        }

        // GET: api/StockPrice/AAPL (Real-time from API)
        [HttpGet("{ticker}")]
        public async Task<ActionResult<decimal?>> GetPrice(string ticker)
        {
            var price = await _stockPriceService.GetCurrentPrice(ticker);

            if (price == null)
            {
                return NotFound($"Could not fetch price for {ticker}");
            }

            return Ok(new { ticker, price });
        }

        // POST: api/StockPrice/multiple
        [HttpPost("multiple")]
        public async Task<ActionResult> GetMultiplePrices([FromBody] List<string> tickers)
        {
            var prices = await _stockPriceService.GetMultiplePrices(tickers);
            return Ok(prices);
        }

        // GET: api/StockPrice/db/AAPL (From Database - Background Job)
        [HttpGet("db/{ticker}")]
        public async Task<ActionResult> GetPriceFromDatabase(string ticker)
        {
            var stockPrice = await _context.StockPrices
                .FirstOrDefaultAsync(sp => sp.Ticker.ToUpper() == ticker.ToUpper());

            if (stockPrice == null)
            {
                return NotFound($"No price data found for {ticker}");
            }

            return Ok(new
            {
                ticker = stockPrice.Ticker,
                price = stockPrice.Price,
                updatedAt = stockPrice.UpdatedAt
            });
        }

        // GET: api/StockPrice/db (All prices from Database)
        [HttpGet("db")]
        public async Task<ActionResult> GetAllPricesFromDatabase()
        {
            var prices = await _context.StockPrices
                .OrderBy(sp => sp.Ticker)
                .ToListAsync();

            return Ok(prices);
        }
    }
}