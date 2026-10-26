using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PizzeriaPadua.API.Services
{
    public class VonageSmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VonageSmsService> _logger;

        public VonageSmsService(HttpClient httpClient, IConfiguration configuration, ILogger<VonageSmsService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendAsync(string to, string message)
        {
            var apiKey = _configuration["Vonage:ApiKey"];
            var apiSecret = _configuration["Vonage:ApiSecret"];
            var from = _configuration["Vonage:From"] ?? "PizzeriaPadua";

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            {
                _logger.LogInformation("Simulando envío de SMS a {To}: {Message}", to, message);
                return;
            }

            var destination = NormalizePhoneNumber(to);
            if (string.IsNullOrWhiteSpace(destination))
                throw new InvalidOperationException("El teléfono no es válido");

            using var request = new HttpRequestMessage(HttpMethod.Post, "sms/json");
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:{apiSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["from"] = from,
                ["to"] = destination,
                ["text"] = message,
                ["type"] = "unicode"
            });

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new Exception("No se pudo enviar el SMS con Vonage");
        }

        private static string NormalizePhoneNumber(string phoneNumber)
        {
            var cleaned = new string(phoneNumber.Where(char.IsDigit).ToArray());
            if (cleaned.Length == 10) return $"52{cleaned}";
            if (cleaned.Length == 12 && cleaned.StartsWith("52")) return cleaned;
            return cleaned;
        }
    }
}
