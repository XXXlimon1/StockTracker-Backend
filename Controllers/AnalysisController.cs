using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockTracker.API.Services;

namespace StockTracker.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AnalysisController : ControllerBase
    {
        private readonly TechnicalAnalysisService _analysisService;

        public AnalysisController(TechnicalAnalysisService analysisService)
        {
            _analysisService = analysisService;
        }

        // POST: api/Analysis/rsi
        [HttpPost("rsi")]
        public ActionResult<decimal> CalculateRSI([FromBody] List<decimal> prices, [FromQuery] int period = 14)
        {
            if (prices == null || prices.Count < period + 1)
                return BadRequest($"En az {period + 1} fiyat verisi gerekli.");

            var rsi = _analysisService.CalculateRSI(prices, period);
            return Ok(new { rsi, period });
        }

        // POST: api/Analysis/macd
        [HttpPost("macd")]
        public ActionResult CalculateMACD([FromBody] List<decimal> prices)
        {
            if (prices == null || prices.Count < 26)
                return BadRequest("MACD için en az 26 fiyat verisi gerekli.");

            var (macd, signal, histogram) = _analysisService.CalculateMACD(prices);
            return Ok(new { macd, signal, histogram });
        }

        // POST: api/Analysis/bollinger
        [HttpPost("bollinger")]
        public ActionResult CalculateBollingerBands([FromBody] List<decimal> prices, [FromQuery] int period = 20)
        {
            if (prices == null || prices.Count < period)
                return BadRequest($"Bollinger Bands için en az {period} fiyat verisi gerekli.");

            var (upper, middle, lower) = _analysisService.CalculateBollingerBands(prices, period);
            return Ok(new { upper, middle, lower });
        }
    }
}
