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
            var credential = GoogleCredential.FromFile("/app/firebase-service-account.json")
                .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");

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
                _logger.LogError(ex, "FCM notification error");
            }
        }
    }
}
