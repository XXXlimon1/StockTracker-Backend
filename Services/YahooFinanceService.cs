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
            var result = await GetBulkPrices(new List<string> { ticker });
            return result.ContainsKey(ticker) ? result[ticker] : null;
        }

        public async Task<(decimal Price, decimal Change, decimal ChangePercent)?> GetPriceWithChange(string ticker)
        {
            var price = await GetPrice(ticker);
            if (!price.HasValue) return null;
            return (price.Value, 0m, 0m);
        }

        // Bulk fiyat çekme - tek istekte birden fazla ticker
        public async Task<Dictionary<string, decimal>> GetBulkPrices(List<string> tickers)
        {
            var result = new Dictionary<string, decimal>();
            if (tickers.Count == 0) return result;

            try
            {
                var symbols = string.Join(",", tickers);
                var url = $"https://query1.finance.yahoo.com/v7/finance/quote?symbols={symbols}&fields=regularMarketPrice";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Yahoo bulk request failed: {response.StatusCode} for {symbols}");
                    return result;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var quotes = doc.RootElement
                    .GetProperty("quoteResponse")
                    .GetProperty("result");

                foreach (var quote in quotes.EnumerateArray())
                {
                    if (quote.TryGetProperty("symbol", out var sym) &&
                        quote.TryGetProperty("regularMarketPrice", out var price))
                    {
                        var ticker = sym.GetString()!;
                        var priceVal = price.GetDecimal();
                        result[ticker] = priceVal;
                        _logger.LogInformation($"Yahoo Finance: {ticker} = ₺{priceVal}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Yahoo bulk error: {ex.Message}");
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
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

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
