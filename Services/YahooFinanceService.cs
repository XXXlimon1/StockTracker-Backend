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
                // THYAO.IS formatında gönder
                var symbol = ticker.Contains(".IS") ? ticker : ticker + ".IS";
                var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?interval=1d&range=1d";

                _logger.LogInformation($"Fetching price for {symbol} from Yahoo Finance");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Yahoo Finance returned {response.StatusCode} for {symbol}");
                    return null;
                }

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

        public async Task<Dictionary<string, decimal?>> GetMultiplePrices(List<string> tickers)
        {
            var result = new Dictionary<string, decimal?>();
            foreach (var ticker in tickers)
            {
                result[ticker] = await GetPrice(ticker);
                await Task.Delay(500); // Yahoo'ya çok hızlı istek atmayalım
            }
            return result;
        }
    }
}
