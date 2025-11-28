using System.Text.Json;

namespace StockTracker.API.Services
{
    public class TwelveDataService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<TwelveDataService> _logger;

        public TwelveDataService(HttpClient httpClient, IConfiguration configuration, ILogger<TwelveDataService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["TwelveData:ApiKey"]!;
            _logger = logger;
        }

        public async Task<decimal?> GetBistPrice(string ticker)
        {
            try
            {
                // THYAO.IS -> THYAO
                var symbol = ticker.Replace(".IS", "");

                var url = $"https://api.twelvedata.com/time_series?symbol={symbol}&interval=1min&country=Turkey&apikey={_apiKey}&outputsize=1";

                _logger.LogInformation($"Fetching price for {symbol} from Twelve Data");

                var response = await _httpClient.GetStringAsync(url);
                var json = JsonDocument.Parse(response);

                // Check for error
                if (json.RootElement.TryGetProperty("status", out var status) &&
                    status.GetString() == "error")
                {
                    _logger.LogWarning($"Twelve Data API error for {symbol}");
                    return null;
                }

                // Get price from values array
                if (json.RootElement.TryGetProperty("values", out var values) &&
                    values.GetArrayLength() > 0)
                {
                    var closePrice = values[0].GetProperty("close").GetString();

                    if (decimal.TryParse(closePrice, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var price))
                    {
                        _logger.LogInformation($"Twelve Data: {symbol} = ₺{price}");
                        return price;
                    }
                }

                _logger.LogWarning($"No price data found for {symbol}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching {ticker} from Twelve Data");
                return null;
            }
        }
    }
}