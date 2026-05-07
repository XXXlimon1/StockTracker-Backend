using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;

namespace StockTracker.API.Services
{
    public class FcmService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FcmService> _logger;
        private readonly string _projectId = "stocktracker-d747b";

        public FcmService(HttpClient httpClient, ILogger<FcmService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        private async Task<string> GetAccessToken()
        {
            GoogleCredential credential;

            var privateKey = Environment.GetEnvironmentVariable("FIREBASE_PRIVATE_KEY");
            var clientEmail = Environment.GetEnvironmentVariable("FIREBASE_CLIENT_EMAIL");

            if (!string.IsNullOrEmpty(privateKey) && !string.IsNullOrEmpty(clientEmail))
            {
                _logger.LogInformation($"Using env vars. Key length: {privateKey.Length}");

                // Render'da \n literal gelir, gerçek newline'a çevir
                privateKey = privateKey.Replace("\\n", "\n");

                // Service account JSON'ı programatik olarak oluştur
                var serviceAccountJson = $@"{{
                    ""type"": ""service_account"",
                    ""project_id"": ""{_projectId}"",
                    ""private_key_id"": ""96edf941705b952c5721558a61a45066ffe6d91c"",
                    ""private_key"": ""{privateKey.Replace("\n", "\\n").Replace("\"", "\\\"")}"",
                    ""client_email"": ""{clientEmail}"",
                    ""client_id"": ""101704412167699795740"",
                    ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
                    ""token_uri"": ""https://oauth2.googleapis.com/token""
                }}";

                credential = GoogleCredential.FromJson(serviceAccountJson)
                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
            }
            else
            {
                // Lokal geliştirme için dosyadan oku
                var path = File.Exists("firebase-service-account.json")
                    ? "firebase-service-account.json"
                    : "/app/firebase-service-account.json";
                _logger.LogInformation($"Reading from file: {path}");
                credential = GoogleCredential.FromFile(path)
                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
            }

            var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
            return token;
        }

        public async Task SendNotification(string fcmToken, string title, string body)
        {
            try
            {
                var accessToken = await GetAccessToken();

                var message = new
                {
                    message = new
                    {
                        token = fcmToken,
                        notification = new { title, body },
                        android = new
                        {
                            priority = "high",
                            notification = new
                            {
                                sound = "default",
                                channel_id = "stock_alerts"
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(message);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                var url = $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send";
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                    _logger.LogInformation($"FCM notification sent: {title}");
                else
                    _logger.LogWarning($"FCM failed: {await response.Content.ReadAsStringAsync()}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"FCM notification error: {ex.Message} | Inner: {ex.InnerException?.Message}");
            }
        }
    }
}
