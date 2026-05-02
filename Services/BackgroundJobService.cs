using StockTracker.API.Data;
using StockTracker.API.Models;
using Microsoft.EntityFrameworkCore;

namespace StockTracker.API.Services
{
    public class BackgroundJobService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundJobService> _logger;

        private readonly List<string> _popularTickers = new()
        {
            "THYAO.IS", "ASELS.IS", "TUPRS.IS", "TCELL.IS", "SAHOL.IS",
            "KCHOL.IS", "BIMAS.IS", "FROTO.IS", "EREGL.IS"
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
            var yahooService = scope.ServiceProvider.GetRequiredService<YahooFinanceService>();
            var fcmService = scope.ServiceProvider.GetRequiredService<FcmService>();

            foreach (var ticker in _popularTickers)
            {
                try
                {
                    var price = await yahooService.GetPrice(ticker);

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

                        context.StockPriceHistories.Add(new StockPriceHistory
                        {
                            Ticker = ticker,
                            Price = price.Value,
                            RecordedAt = DateTime.UtcNow
                        });

                        _logger.LogInformation($"Updated {ticker}: ₺{price.Value}");
                        await CheckPriceAlerts(context, fcmService, ticker, price.Value);
                    }
                    else
                    {
                        _logger.LogWarning($"Could not fetch price for {ticker}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating {ticker}");
                }

                await Task.Delay(1000);
            }

            await context.SaveChangesAsync();
            await CleanOldHistory(context);

            _logger.LogInformation("Background job completed");
        }

        private async Task CheckPriceAlerts(AppDbContext context, FcmService fcmService, string ticker, decimal currentPrice)
        {
            var activeAlerts = await context.Alerts
                .Include(a => a.User)
                .Where(a => a.Ticker == ticker && a.IsActive)
                .ToListAsync();

            foreach (var alert in activeAlerts)
            {
                bool triggered = alert.AlertType switch
                {
                    "PRICE_ABOVE" => currentPrice >= alert.TargetValue,
                    "PRICE_BELOW" => currentPrice <= alert.TargetValue,
                    _ => false
                };

                if (triggered)
                {
                    alert.IsActive = false;
                    alert.TriggeredAt = DateTime.UtcNow;

                    _logger.LogInformation($"Alert triggered! UserId={alert.UserId}, {ticker} {alert.AlertType} {alert.TargetValue}");

                    // User'ı ayrıca çek - FcmToken için
                    var user = await context.Users.FindAsync(alert.UserId);
                    _logger.LogInformation($"User found: {user?.Email}, FcmToken: {(user?.FcmToken != null ? "exists" : "null")}");

                    if (user?.FcmToken != null)
                    {
                        var cleanTicker = ticker.Replace(".IS", "");
                        var direction = alert.AlertType == "PRICE_ABOVE" ? "üstüne" : "altına";
                        var title = $"🔔 {cleanTicker} Uyarısı";
                        var body = $"{cleanTicker} hedef fiyat {direction} geçti! Anlık: ₺{currentPrice}";

                        _logger.LogInformation($"Sending FCM to {user.Email}...");
                        await fcmService.SendNotification(user.FcmToken, title, body);
                    }
                }
            }
        }

        private async Task CleanOldHistory(AppDbContext context)
        {
            var cutoff = DateTime.UtcNow.AddDays(-30);
            var old = context.StockPriceHistories.Where(h => h.RecordedAt < cutoff);
            context.StockPriceHistories.RemoveRange(old);
            await context.SaveChangesAsync();
        }
    }
}
