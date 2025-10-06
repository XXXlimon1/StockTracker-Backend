namespace StockTracker.API.Models
{
    public class Stock
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public DateTime PurchaseDate { get; set; }

        // Navigation property
        public User? User { get; set; }
    }
}