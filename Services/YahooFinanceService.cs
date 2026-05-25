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
            try
            {
                var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?interval=1d&range=1d";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var price = doc.RootElement
                    .GetProperty("chart")
                    .GetProperty("result")[0]
                    .GetProperty("meta")
                    .GetProperty("regularMarketPrice")
                    .GetDecimal();

                _logger.LogInformation($"Yahoo Finance: {ticker} = ₺{price}");
                return price;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Yahoo Finance error for {ticker}: {ex.Message}");
                return null;
            }
        }

        public async Task<(decimal Price, decimal Change, decimal ChangePercent)?> GetPriceWithChange(string ticker)
        {
            var price = await GetPrice(ticker);
            if (!price.HasValue) return null;
            return (price.Value, 0m, 0m);
        }

        public async Task<List<(DateTime Date, decimal Close)>> GetHistoricalPrices(string ticker, string range = "1mo")
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
