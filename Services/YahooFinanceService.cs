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
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _logger = logger;
        }

        public async Task<decimal?> GetPrice(string ticker)
        {
            try
            {
                var symbol = ticker.Contains(".IS") ? ticker : ticker + ".IS";
                var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?interval=1d&range=1d";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                var content = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(content);

                var price = json.RootElement
                    .GetProperty("chart")
                    .GetProperty("result")[0]
                    .GetProperty("meta")
                    .GetProperty("regularMarketPrice")
                    .GetDecimal();

                _logger.LogInformation($"Yahoo Finance: {symbol} = ₺{price}");
                return price;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching {ticker} from Yahoo Finance");
                return null;
            }
        }

        // Geçmiş fiyat verisi - grafik ve teknik analiz için
        public async Task<List<(DateTime Date, decimal Close)>> GetHistoricalPrices(string ticker, string range = "1mo")
        {
            try
            {
                var symbol = ticker.Contains(".IS") ? ticker : ticker + ".IS";
                // interval=1d, range: 5d, 1mo, 3mo, 6mo, 1y
                var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?interval=1d&range={range}";

                _logger.LogInformation($"Fetching historical data for {symbol}, range={range}");

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Yahoo Finance historical request failed: {response.StatusCode}");
                    return new List<(DateTime, decimal)>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(content);

                var result = json.RootElement
                    .GetProperty("chart")
                    .GetProperty("result")[0];

                var timestamps = result.GetProperty("timestamp").EnumerateArray()
                    .Select(t => DateTimeOffset.FromUnixTimeSeconds(t.GetInt64()).UtcDateTime)
                    .ToList();

                var closes = result
                    .GetProperty("indicators")
                    .GetProperty("quote")[0]
                    .GetProperty("close")
                    .EnumerateArray()
                    .Select(c => c.ValueKind == JsonValueKind.Null ? 0m : c.GetDecimal())
                    .ToList();

                var data = timestamps.Zip(closes, (date, close) => (date, close))
                    .Where(x => x.close > 0)
                    .ToList();

                _logger.LogInformation($"Fetched {data.Count} historical prices for {symbol}");
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching historical data for {ticker}");
                return new List<(DateTime, decimal)>();
            }
        }

        public async Task<Dictionary<string, decimal?>> GetMultiplePrices(List<string> tickers)
        {
            var result = new Dictionary<string, decimal?>();
            foreach (var ticker in tickers)
            {
                result[ticker] = await GetPrice(ticker);
                await Task.Delay(500);
            }
            return result;
        }
    }
}
