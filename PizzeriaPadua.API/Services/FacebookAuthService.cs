using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PizzeriaPadua.API.Services
{
    public class FacebookSettings
    {
        public string AppId { get; set; } = "";
        public string AppSecret { get; set; } = "";
    }

    public interface IFacebookAuthService
    {
        Task<FacebookUserInfo?> ValidateTokenAsync(string accessToken);
    }

    public class FacebookUserInfo
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public string Picture { get; set; } = "";
    }

    public class FacebookAuthService : IFacebookAuthService
    {
        private readonly FacebookSettings _settings;
        private readonly HttpClient _http;
        private readonly ILogger<FacebookAuthService> _log;

        public FacebookAuthService(IOptions<FacebookSettings> settings, HttpClient http, ILogger<FacebookAuthService> log)
        {
            _settings = settings.Value;
            _http = http;
            _log = log;
        }

        public async Task<FacebookUserInfo?> ValidateTokenAsync(string accessToken)
        {
            try
            {
                var url = $"https://graph.facebook.com/me?fields=id,name,email,picture.type(large)&access_token={accessToken}";
                var response = await _http.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _log.LogWarning("Facebook validation failed: {Status}", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                return new FacebookUserInfo
                {
                    Id = root.GetProperty("id").GetString() ?? "",
                    Name = root.GetProperty("name").GetString() ?? "",
                    Email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() ?? "" : "",
                    Picture = root.TryGetProperty("picture", out var picProp) &&
                              picProp.TryGetProperty("data", out var dataProp) &&
                              dataProp.TryGetProperty("url", out var urlProp)
                        ? urlProp.GetString() ?? ""
                        : ""
                };
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error validating Facebook token");
                return null;
            }
        }
    }
}
