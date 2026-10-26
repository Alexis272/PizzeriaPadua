using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PizzeriaPadua.API.Services
{
    public class LicenseData
    {
        public string? ExpiryDate { get; set; }
        public string? IssuedAt { get; set; }
        public string Signature { get; set; } = "";
    }

    public class LicensingService
    {
        private const string Key = "PizzeriaPaduaLicenseKey2026!Secure";
        private static readonly string LicensePath = Path.Combine(AppContext.BaseDirectory, "license.lic");
        private LicenseData _license = new();
        private readonly ILogger<LicensingService> _logger;

        public LicensingService(ILogger<LicensingService> logger)
        {
            _logger = logger;
            LoadOrEnsureDefaultLicense();
        }

        private DateTime? ParseDate(string? raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            if (DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                return dt;
            return null;
        }

        public bool IsLicensed
        {
            get
            {
                var expiry = ParseDate(_license.ExpiryDate);
                if (expiry == null) return false;
                if (DateTime.UtcNow > expiry.Value) return false;
                return VerifySignature();
            }
        }

        public DateTime? ExpiryDate => ParseDate(_license.ExpiryDate);

        public string? ExpiryDateFormatted
        {
            get
            {
                var dt = ParseDate(_license.ExpiryDate);
                if (dt == null) return null;
                return dt.Value.ToString("dd/MM/yyyy HH:mm");
            }
        }

        public bool IsExpired
        {
            get
            {
                var expiry = ParseDate(_license.ExpiryDate);
                if (expiry == null) return false;
                return DateTime.UtcNow > expiry.Value;
            }
        }

        public bool UploadLicense(string content)
        {
            try
            {
                var data = JsonSerializer.Deserialize<LicenseData>(content);
                if (data == null) return false;

                if (string.IsNullOrEmpty(data.ExpiryDate) || string.IsNullOrEmpty(data.IssuedAt) || string.IsNullOrEmpty(data.Signature))
                    return false;

                if (ParseDate(data.ExpiryDate) == null)
                {
                    _logger.LogWarning("License upload failed: could not parse ExpiryDate '{Expiry}'", data.ExpiryDate);
                    return false;
                }

                _license = data;
                File.WriteAllText(LicensePath, content);
                _logger.LogInformation("License uploaded, expires: {Expiry}", data.ExpiryDate);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload license");
                return false;
            }
        }

        public void RevokeLicense()
        {
            _license = new LicenseData();
            try
            {
                if (File.Exists(LicensePath))
                    File.Delete(LicensePath);
                _logger.LogInformation("License revoked");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete license file");
            }
        }

        private void LoadOrEnsureDefaultLicense()
        {
            try
            {
                if (File.Exists(LicensePath))
                {
                    var json = File.ReadAllText(LicensePath);
                    var data = JsonSerializer.Deserialize<LicenseData>(json);
                    if (data != null)
                    {
                        _license = data;
                        if (IsLicensed) return;
                    }
                }

                var issued = DateTime.UtcNow.ToString("O");
                var expiry = DateTime.UtcNow.AddDays(365).ToString("O");
                var raw = $"{expiry}|{issued}";
                var sig = Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Key), Encoding.UTF8.GetBytes(raw)));
                _license = new LicenseData { ExpiryDate = expiry, IssuedAt = issued, Signature = sig };
                File.WriteAllText(LicensePath, JsonSerializer.Serialize(_license, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load or create default license file");
            }
        }

        private bool VerifySignature()
        {
            if (string.IsNullOrEmpty(_license.ExpiryDate) || string.IsNullOrEmpty(_license.IssuedAt)) return false;
            string data = $"{_license.ExpiryDate}|{_license.IssuedAt}";
            byte[] keyBytes = Encoding.UTF8.GetBytes(Key);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] hash = HMACSHA256.HashData(keyBytes, dataBytes);
            string expected = Convert.ToBase64String(hash);
            return expected == _license.Signature;
        }
    }
}
