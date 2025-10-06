namespace StockTracker.API.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "BUY" or "SELL"
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;

        // Navigation property
        public User? User { get; set; }
    }
}