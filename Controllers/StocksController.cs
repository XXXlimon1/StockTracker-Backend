using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTracker.API.Data;
using StockTracker.API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace StockTracker.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StocksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StocksController(AppDbContext context)
        {
            _context = context;
        }

        // JWT token'dan userId'yi güvenli şekilde al
        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var userId) ? userId : 0;
        }

        // GET: api/Stocks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Stock>>> GetMyStocks()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            return await _context.Stocks
                .Where(s => s.UserId == userId)
                .ToListAsync();
        }

        // GET: api/Stocks/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Stock>> GetStock(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (stock == null) return NotFound();

            return stock;
        }

        // GET: api/Stocks/summary — Portfolio özeti (Android ana ekranı için)
        [HttpGet("summary")]
        public async Task<ActionResult> GetPortfolioSummary()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var stocks = await _context.Stocks
                .Where(s => s.UserId == userId)
                .ToListAsync();

            if (!stocks.Any())
            {
                return Ok(new
                {
                    totalCost = 0m,
                    totalCurrentValue = 0m,
                    totalProfitLoss = 0m,
                    totalProfitLossPercent = 0m,
                    stockCount = 0,
                    stocks = new List<object>()
                });
            }

            // Her hisse için anlık fiyatı DB'den çek (background job güncelliyor)
            var tickers = stocks.Select(s => s.Ticker).Distinct().ToList();
            var currentPrices = await _context.StockPrices
                .Where(sp => tickers.Contains(sp.Ticker))
                .ToDictionaryAsync(sp => sp.Ticker, sp => sp.Price);

            var stockDetails = stocks.Select(stock =>
            {
                var ticker = stock.Ticker.Replace(".IS", ""); // THYAO.IS -> THYAO
                var currentPrice = currentPrices.TryGetValue(stock.Ticker, out var p) ? p : stock.PurchasePrice;
                var cost = stock.PurchasePrice * stock.Quantity;
                var currentValue = currentPrice * stock.Quantity;
                var profitLoss = currentValue - cost;
                var profitLossPercent = cost > 0 ? (profitLoss / cost) * 100 : 0;

                return new
                {
                    id = stock.Id,
                    ticker = ticker,
                    fullTicker = stock.Ticker,
                    quantity = stock.Quantity,
                    purchasePrice = stock.PurchasePrice,
                    currentPrice = currentPrice,
                    cost = Math.Round(cost, 2),
                    currentValue = Math.Round(currentValue, 2),
                    profitLoss = Math.Round(profitLoss, 2),
                    profitLossPercent = Math.Round(profitLossPercent, 2),
                    purchaseDate = stock.PurchaseDate
                };
            }).ToList();

            var totalCost = stockDetails.Sum(s => s.cost);
            var totalCurrentValue = stockDetails.Sum(s => s.currentValue);
            var totalProfitLoss = totalCurrentValue - totalCost;
            var totalProfitLossPercent = totalCost > 0 ? (totalProfitLoss / totalCost) * 100 : 0;

            return Ok(new
            {
                totalCost = Math.Round(totalCost, 2),
                totalCurrentValue = Math.Round(totalCurrentValue, 2),
                totalProfitLoss = Math.Round(totalProfitLoss, 2),
                totalProfitLossPercent = Math.Round(totalProfitLossPercent, 2),
                stockCount = stocks.Count,
                stocks = stockDetails
            });
        }

        // POST: api/Stocks
        [HttpPost]
        public async Task<ActionResult<Stock>> PostStock(Stock stock)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            // userId her zaman token'dan gelsin, body'den değil
            stock.UserId = userId;

            _context.Stocks.Add(stock);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStock), new { id = stock.Id }, stock);
        }

        // PUT: api/Stocks/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStock(int id, Stock stock)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var existing = await _context.Stocks
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (existing == null) return NotFound();

            existing.Ticker = stock.Ticker;
            existing.Quantity = stock.Quantity;
            existing.PurchasePrice = stock.PurchasePrice;
            existing.PurchaseDate = stock.PurchaseDate;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Stocks/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStock(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (stock == null) return NotFound();

            _context.Stocks.Remove(stock);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
