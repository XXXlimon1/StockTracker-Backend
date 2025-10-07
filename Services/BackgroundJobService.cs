using StockTracker.API.Data;
using StockTracker.API.Models;
using Microsoft.EntityFrameworkCore;

namespace StockTracker.API.Services
{
    public class BackgroundJobService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundJobService> _logger;

        // Popüler BIST 100 hisseleri (örnek 20 adet)
        private readonly List<string> _popularTickers = new()
        {
            "AAPL", "GOOGL", "MSFT", "AMZN", "TSLA",
            "META", "NVDA", "JPM", "V", "WMT",
            "JNJ", "PG", "MA", "HD", "DIS",
            "PYPL", "NFLX", "ADBE", "CRM", "INTC"
        };

        public BackgroundJobService(IServiceProvider serviceProvider, ILogger<BackgroundJobService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task UpdateStockPrices()
        {
            _logger.LogInformation("Background job started: Updating stock prices");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stockPriceService = scope.ServiceProvider.GetRequiredService<StockPriceService>();

            foreach (var ticker in _popularTickers)
            {
                try
                {
                    var price = await stockPriceService.GetCurrentPrice(ticker);

                    if (price.HasValue)
                    {
                        var existingPrice = await context.StockPrices
                            .FirstOrDefaultAsync(sp => sp.Ticker == ticker);

                        if (existingPrice != null)
                        {
                            existingPrice.Price = price.Value;
                            existingPrice.UpdatedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            context.StockPrices.Add(new StockPrice
                            {
                                Ticker = ticker,
                                Price = price.Value,
                                UpdatedAt = DateTime.UtcNow
                            });
                        }

                        _logger.LogInformation($"Updated price for {ticker}: ${price.Value}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating price for {ticker}");
                }

                // API rate limit için kısa bekleme
                await Task.Delay(100);
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("Background job completed: Stock prices updated");
        }
    }
}