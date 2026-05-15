namespace StockTracker.API.Models
{
    public class Watchlist
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public User? User { get; set; }
    }
}