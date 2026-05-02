namespace StockTracker.API.Services
{
    public class TechnicalAnalysisService
    {
        // Wilder's Smoothed RSI - tüm veriyi kullanarak doğru hesaplama
        public decimal CalculateRSI(List<decimal> prices, int period = 14)
        {
            if (prices.Count < period + 1)
                return 0;

            // İlk ortalama kazanç/kayıp
            decimal avgGain = 0;
            decimal avgLoss = 0;

            for (int i = 1; i <= period; i++)
            {
                var change = prices[i] - prices[i - 1];
                if (change > 0) avgGain += change;
                else avgLoss += Math.Abs(change);
            }

            avgGain /= period;
            avgLoss /= period;

            // Wilder'ın smoothed moving average ile devam et
            for (int i = period + 1; i < prices.Count; i++)
            {
                var change = prices[i] - prices[i - 1];
                var gain = change > 0 ? change : 0;
                var loss = change < 0 ? Math.Abs(change) : 0;

                avgGain = (avgGain * (period - 1) + gain) / period;
                avgLoss = (avgLoss * (period - 1) + loss) / period;
            }

            if (avgLoss == 0) return 100;

            var rs = avgGain / avgLoss;
            var rsi = 100 - (100 / (1 + rs));

            return Math.Round(rsi, 2);
        }

        public (decimal macd, decimal signal, decimal histogram) CalculateMACD(
            List<decimal> prices,
            int fastPeriod = 12,
            int slowPeriod = 26,
            int signalPeriod = 9)
        {
            if (prices.Count < slowPeriod)
                return (0, 0, 0);

            var fastEMA = CalculateEMA(prices, fastPeriod);
            var slowEMA = CalculateEMA(prices, slowPeriod);
            var macdLine = fastEMA - slowEMA;

            var macdValues = new List<decimal> { macdLine };
            var signalLine = CalculateEMA(macdValues, signalPeriod);
            var histogram = macdLine - signalLine;

            return (
                Math.Round(macdLine, 2),
                Math.Round(signalLine, 2),
                Math.Round(histogram, 2)
            );
        }

        public (decimal upper, decimal middle, decimal lower) CalculateBollingerBands(
            List<decimal> prices,
            int period = 20,
            decimal multiplier = 2)
        {
            if (prices.Count < period)
                return (0, 0, 0);

            var recentPrices = prices.TakeLast(period).ToList();
            var sma = recentPrices.Average();

            var sumSquaredDiff = recentPrices.Sum(p => (p - sma) * (p - sma));
            var standardDeviation = (decimal)Math.Sqrt((double)(sumSquaredDiff / period));

            return (
                Math.Round(sma + (multiplier * standardDeviation), 2),
                Math.Round(sma, 2),
                Math.Round(sma - (multiplier * standardDeviation), 2)
            );
        }

        private decimal CalculateEMA(List<decimal> prices, int period)
        {
            if (prices.Count == 0) return 0;
            if (prices.Count < period) return prices.Average();

            var multiplier = 2m / (period + 1);
            var ema = prices.Take(period).Average();

            foreach (var price in prices.Skip(period))
            {
                ema = (price - ema) * multiplier + ema;
            }

            return ema;
        }
    }
}
