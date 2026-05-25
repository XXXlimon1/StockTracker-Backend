using System.Text.Json;

namespace StockTracker.API.Services
{
    public class YahooFinanceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<YahooFinanceService> _logger;

        public YahooFinanceService(HttpClient httpClient, ILogger<YahooFinanceService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<decimal?> GetPrice(string ticker)
        {
            var result = await GetPriceWithChange(ticker);
            return result?.Price;
        }

        public async Task<(decimal Price, decimal Change, decimal ChangePercent)?> GetPriceWithChange(string ticker)
        {
            try
            {
                var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?interval=1d&range=2d";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var root = doc.RootElement;
                var result = root.GetProperty("chart").GetProperty("result")[0];
                var meta = result.GetProperty("meta");

                var currentPrice = meta.GetProperty("regularMarketPrice").GetDecimal();
                
                decimal previousClose = 0;
                if (meta.TryGetProperty("chartPreviousClose", out var prev1))
                    previousClose = prev1.GetDecimal();
                else if (meta.TryGetProperty("previousClose", out var prev2))
                    previousClose = prev2.GetDecimal();
                else if (meta.TryGetProperty("regularMarketPreviousClose", out var prev3))
                    previousClose = prev3.GetDecimal();

                var change = previousClose != 0 ? currentPrice - previousClose : 0;
                var changePercent = previousClose != 0 ? (change / previousClose) * 100 : 0;

                _logger.LogInformation($"Yahoo Finance: {ticker} = ₺{currentPrice} ({changePercent:+0.00;-0.00}%)");

                return (currentPrice, change, changePercent);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Yahoo Finance error for {ticker}: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        // Alias for backward compatibility
        public async Task<List<(DateTime Date, decimal Close)>> GetHistoricalPrices(string ticker, string range = "1mo")
            => await GetHistory(ticker, range);

        public async Task<List<(DateTime Date, decimal Close)>> GetHistory(string ticker, string range = "1mo")
        {
            var result = new List<(DateTime, decimal)>();
            try
            {
                var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?interval=1d&range={range}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return result;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var chartResult = doc.RootElement
                    .GetProperty("chart")
                    .GetProperty("result")[0];

                var timestamps = chartResult.GetProperty("timestamp").EnumerateArray().ToList();
                var closes = chartResult
                    .GetProperty("indicators")
                    .GetProperty("quote")[0]
                    .GetProperty("close")
                    .EnumerateArray()
                    .ToList();

                for (int i = 0; i < Math.Min(timestamps.Count, closes.Count); i++)
                {
                    if (closes[i].ValueKind == JsonValueKind.Null) continue;
                    var date = DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64()).UtcDateTime;
                    var close = closes[i].GetDecimal();
                    result.Add((date, close));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Yahoo history error for {ticker}: {ex.Message}");
            }
            return result;
        }
    }
}
