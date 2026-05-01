namespace StockTracker.API.Models
{
    public class StockPriceHistory
    {
        public int Id { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
