using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using System.Security.Cryptography;

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
            var privateKey = Environment.GetEnvironmentVariable("FIREBASE_PRIVATE_KEY");
            var clientEmail = Environment.GetEnvironmentVariable("FIREBASE_CLIENT_EMAIL");

            if (!string.IsNullOrEmpty(privateKey) && !string.IsNullOrEmpty(clientEmail))
            {
                // \n literal -> gerçek newline
                privateKey = privateKey.Replace("\\n", "\n");
                _logger.LogInformation($"Key length after replace: {privateKey.Length}, starts with: {privateKey.Substring(0, 30)}");

                var credential = new ServiceAccountCredential(
                    new ServiceAccountCredential.Initializer(clientEmail)
                    {
                        Scopes = new[] { "https://www.googleapis.com/auth/firebase.messaging" }
                    }.FromPrivateKey(privateKey));

                var token = await credential.GetAccessTokenForRequestAsync();
                return token;
            }
            else
            {
                var path = File.Exists("firebase-service-account.json")
                    ? "firebase-service-account.json"
                    : "/app/firebase-service-account.json";
                _logger.LogInformation($"Reading from file: {path}");
                var credential = GoogleCredential.FromFile(path)
                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
                return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
            }
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

                var request = new HttpRequestMessage(HttpMethod.Post,
                    $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    _logger.LogInformation($"FCM notification sent: {title}");
                else
                    _logger.LogWarning($"FCM failed: {responseBody}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"FCM error: {ex.Message}");
            }
        }
    }
}
