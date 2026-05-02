namespace StockTracker.API.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? FcmToken { get; set; } // Push notification için
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
