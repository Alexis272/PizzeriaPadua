using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PizzeriaPadua.API.Services
{
    public class SendGridSettings
    {
        public string ApiKey { get; set; } = "";
        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "PizzeriaPadua";
    }

    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly SendGridSettings _settings;
        private readonly HttpClient _http;
        private readonly ILogger<EmailService> _log;

        public EmailService(IOptions<SendGridSettings> settings, HttpClient http, ILogger<EmailService> log)
        {
            _settings = settings.Value;
            _http = http;
            _log = log;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _log.LogInformation("Simulando envío de correo a {To}: {Subject}", to, subject);
                return;
            }

            var payload = new
            {
                personalizations = new[] { new { to = new[] { new { email = to } } } },
                from = new { email = _settings.FromEmail, name = _settings.FromName },
                subject,
                content = new[] { new { type = "text/html", value = body } }
            };

            var json = JsonSerializer.Serialize(payload);
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send")
            {
                Headers = { { "Authorization", $"Bearer {_settings.ApiKey}" } },
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var res = await _http.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                _log.LogError("SendGrid error {Status}: {Body}", res.StatusCode, err);
                throw new Exception($"SendGrid respondió con {res.StatusCode}");
            }
        }
    }
}
