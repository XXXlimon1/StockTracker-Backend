using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace StockTracker.API.Services
{
    public class StockPriceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StockPriceService> _logger;
        private readonly IMemoryCache _cache;
        private const int CacheExpirationMinutes = 5;

        public StockPriceService(HttpClient httpClient, ILogger<StockPriceService> logger, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
        }

        public async Task<decimal?> GetCurrentPrice(string ticker)
        {
            // Cache'den kontrol et
            var cacheKey = $"price_{ticker.ToUpper()}";
            if (_cache.TryGetValue(cacheKey, out decimal? cachedPrice))
            {
                _logger.LogInformation($"Returning cached price for {ticker}");
                return cachedPrice;
            }

            try
            {
                var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?interval=1d&range=1d";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Yahoo Finance blocked, returning mock data for {ticker}");
                    var mockPrice = GetMockPrice(ticker);
                    // Mock data'yı da cache'le
                    _cache.Set(cacheKey, mockPrice, TimeSpan.FromMinutes(CacheExpirationMinutes));
                    return mockPrice;
                }

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);

                var price = jsonDoc.RootElement
                    .GetProperty("chart")
                    .GetProperty("result")[0]
                    .GetProperty("meta")
                    .GetProperty("regularMarketPrice")
                    .GetDecimal();

                // Cache'e kaydet
                _cache.Set(cacheKey, price, TimeSpan.FromMinutes(CacheExpirationMinutes));

                return price;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching price for {ticker}, using mock data");
                var mockPrice = GetMockPrice(ticker);
                _cache.Set(cacheKey, mockPrice, TimeSpan.FromMinutes(CacheExpirationMinutes));
                return mockPrice;
            }
        }

        public async Task<Dictionary<string, decimal?>> GetMultiplePrices(List<string> tickers)
        {
            var result = new Dictionary<string, decimal?>();

            foreach (var ticker in tickers)
            {
                result[ticker] = await GetCurrentPrice(ticker);
            }

            return result;
        }

        private decimal GetMockPrice(string ticker)
        {
            var mockPrices = new Dictionary<string, decimal>
            {
                { "AAPL", 178.25m },
                { "GOOGL", 140.50m },
                { "MSFT", 380.75m },
                { "TSLA", 245.30m },
                { "AMZN", 155.80m }
            };

            return mockPrices.ContainsKey(ticker.ToUpper())
                ? mockPrices[ticker.ToUpper()]
                : 100.00m;
        }
    }
}