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
                var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?interval=1d&range=1d";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36");
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var meta = doc.RootElement
                    .GetProperty("chart")
                    .GetProperty("result")[0]
                    .GetProperty("meta");

                var price = meta.GetProperty("regularMarketPrice").GetDecimal();

                decimal changePercent = 0m;
                decimal change = 0m;

                if (meta.TryGetProperty("regularMarketChangePercent", out var pct))
                    changePercent = pct.GetDecimal();
                if (meta.TryGetProperty("regularMarketChange", out var chg))
                    change = chg.GetDecimal();

                _logger.LogInformation($"Yahoo Finance: {ticker} = ₺{price} ({changePercent:+0.00;-0.00}%)");
                return (price, change, changePercent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Yahoo Finance error for {ticker}: {ex.Message}");
                return null;
            }
        }

        // Batch olarak çek - 5'li gruplar, aralarında bekleme
        public async Task<Dictionary<string, decimal>> GetBulkPrices(List<string> tickers)
        {
            var result = new Dictionary<string, decimal>();
            var batches = tickers.Chunk(5);

            foreach (var batch in batches)
            {
                foreach (var ticker in batch)
                {
                    var price = await GetPrice(ticker);
                    if (price.HasValue)
                        result[ticker] = price.Value;
                }
                await Task.Delay(1500); // Her batch sonrası 1.5 saniye bekle
            }

            return result;
        }

        public async Task<List<(DateTime Date, decimal Close)>> GetHistoricalPrices(string ticker, string range = "1mo")
        {
            var result = new List<(DateTime, decimal)>();
            try
            {
                var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?interval=1d&range={range}";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36");

                var response = await _httpClient.SendAsync(request);
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
