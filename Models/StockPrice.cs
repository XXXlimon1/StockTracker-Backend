namespace StockTracker.API.Models
{
    public class StockPrice
    {
        public int Id { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal ChangePercent { get; set; } = 0m;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}