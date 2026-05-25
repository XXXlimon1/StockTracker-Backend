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
            // BIST 30
            "AKBNK.IS", "ARCLK.IS", "ASELS.IS", "BIMAS.IS", "DOHOL.IS",
            "EKGYO.IS", "EREGL.IS", "FROTO.IS", "GARAN.IS", "HALKB.IS",
            "ISCTR.IS", "KCHOL.IS", "KOZAL.IS", "KRDMD.IS", "MGROS.IS",
            "PETKM.IS", "PGSUS.IS", "SAHOL.IS", "SASA.IS", "SISE.IS",
            "TAVHL.IS", "TCELL.IS", "THYAO.IS", "TKFEN.IS", "TOASO.IS",
            "TSKB.IS", "TTKOM.IS", "TUPRS.IS", "VAKBN.IS", "YKBNK.IS",
            // Ana Pazar
            "AEFES.IS", "ALARK.IS", "ANACM.IS", "BRISA.IS", "CIMSA.IS",
            "DEVA.IS", "DOAS.IS", "ENKAI.IS", "ALKIM.IS", "BAGFS.IS"
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

            // GetBulkPricesWithChange ile hem fiyat hem yüzde çek
            var pricesWithChange = await yahooService.GetBulkPricesWithChange(_popularTickers);
            _logger.LogInformation($"Bulk fetch: {pricesWithChange.Count}/{_popularTickers.Count} prices received");

            foreach (var (ticker, data) in pricesWithChange)
            {
                try
                {
                    var existingPrice = await context.StockPrices
                        .FirstOrDefaultAsync(sp => sp.Ticker == ticker);

                    if (existingPrice != null)
                    {
                        existingPrice.Price = data.Price;
                        existingPrice.ChangePercent = data.ChangePercent;
                        existingPrice.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        context.StockPrices.Add(new StockPrice
                        {
                            Ticker = ticker,
                            Price = data.Price,
                            ChangePercent = data.ChangePercent,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }

                    context.StockPriceHistories.Add(new StockPriceHistory
                    {
                        Ticker = ticker,
                        Price = data.Price,
                        RecordedAt = DateTime.UtcNow
                    });

                    _logger.LogInformation($"Updated {ticker}: ₺{data.Price} ({data.ChangePercent:+0.00;-0.00}%)");
                    await CheckPriceAlerts(context, fcmService, ticker, data.Price);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing {ticker}");
                }
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
