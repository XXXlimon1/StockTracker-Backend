namespace StockTracker.API.Models
{
    public class Alert
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty; // "PRICE_ABOVE", "PRICE_BELOW"
        public decimal TargetValue { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? TriggeredAt { get; set; } // Tetiklenme zamanı

        public User? User { get; set; }
    }
}
