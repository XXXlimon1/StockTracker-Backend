using StockTracker.API.Data;
using StockTracker.API.Models;
using Microsoft.EntityFrameworkCore;

namespace StockTracker.API.Services
{
    public class BackgroundJobService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundJobService> _logger;

        // BIST hisseleri
        private readonly List<string> _popularTickers = new()
        {
             "THYAO.IS",   // Çalışıyor
             "ASELS.IS",   // Aselsan - test et
             "TUPRS.IS",   // Tüpraş - test et  
             "TCELL.IS",   // Turkcell - test et
             "SAHOL.IS",   // Sabancı - test et
             "KCHOL.IS",   // Koç - test et
             "BIMAS.IS",   // BIM - test et
             "FROTO.IS",   // Ford - test et
             "EREGL.IS"    // Ereğli - test et
        };

        public BackgroundJobService(IServiceProvider serviceProvider, ILogger<BackgroundJobService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task UpdateStockPrices()
        {
            _logger.LogInformation("Background job started: Updating stock prices with Twelve Data");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var twelveDataService = scope.ServiceProvider.GetRequiredService<TwelveDataService>();

            foreach (var ticker in _popularTickers)
            {
                try
                {
                    var price = await twelveDataService.GetBistPrice(ticker);

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

                        _logger.LogInformation($"Updated price for {ticker}: ₺{price.Value}");
                    }
                    else
                    {
                        _logger.LogWarning($"Could not fetch price for {ticker}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating price for {ticker}");
                }

                // Rate limit: 8 credits/minute
                // 15 hisse / 8 credit = ~2 dakika
                // Her hisse arası 8 saniye bekle
                await Task.Delay(8000);
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("Background job completed: Stock prices updated");
        }
    }
}