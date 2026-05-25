using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockTracker.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace StockTracker.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AiAnalysisController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private readonly ILogger<AiAnalysisController> _logger;

        public AiAnalysisController(HttpClient httpClient, AppDbContext context, ILogger<AiAnalysisController> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;
        }

        [HttpPost("analyze/{ticker}")]
        public async Task<ActionResult> Analyze(string ticker, [FromBody] AnalyzeRequest request)
        {
            try
            {
                var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
                if (string.IsNullOrEmpty(apiKey))
                    return StatusCode(500, "AI servisi yapılandırılmamış");

                // Prompt oluştur
                var prompt = BuildPrompt(ticker, request);

                // Claude API'ye istek at
                var claudeRequest = new
                {
                    model = "claude-sonnet-4-5",
                    max_tokens = 1000,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    }
                };

                var json = JsonSerializer.Serialize(claudeRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                var response = await _httpClient.PostAsync("https://api.anthropic.com/v1/messages", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Claude API error: {responseBody}");
                    return StatusCode(500, "AI analizi alınamadı");
                }

                using var doc = JsonDocument.Parse(responseBody);
                var text = doc.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString();

                return Ok(new { analysis = text });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI analysis error");
                return StatusCode(500, "Bir hata oluştu");
            }
        }

        private string BuildPrompt(string ticker, AnalyzeRequest request)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Aşağıdaki BIST hissesi için kısa bir teknik analiz yap. Yatırım tavsiyesi değil, teknik göstergelerin yorumudur.");
            sb.AppendLine($"\nHisse: {ticker.Replace(".IS", "")}");
            sb.AppendLine($"Anlık Fiyat: ₺{request.CurrentPrice:F2}");

            if (request.Rsi.HasValue)
                sb.AppendLine($"RSI (14): {request.Rsi:F1} {(request.Rsi >= 70 ? "(Aşırı Alım)" : request.Rsi <= 30 ? "(Aşırı Satım)" : "(Nötr)")}");

            if (request.Macd.HasValue)
                sb.AppendLine($"MACD: {request.Macd:F2} {(request.Macd > 0 ? "(Pozitif)" : "(Negatif)")}");

            if (request.PriceHistory != null && request.PriceHistory.Count > 0)
            {
                var first = request.PriceHistory.First();
                var last = request.PriceHistory.Last();
                var change = ((last - first) / first) * 100;
                sb.AppendLine($"Son {request.PriceHistory.Count} günlük değişim: %{change:F1}");
                sb.AppendLine($"Dönem en düşük: ₺{request.PriceHistory.Min():F2}, en yüksek: ₺{request.PriceHistory.Max():F2}");
            }

            sb.AppendLine("\n3-4 cümle ile teknik görünümü özetle. Türkçe yanıtla.");

            return sb.ToString();
        }
    }

    public class AnalyzeRequest
    {
        public double CurrentPrice { get; set; }
        public double? Rsi { get; set; }
        public double? Macd { get; set; }
        public List<double>? PriceHistory { get; set; }
    }
}
