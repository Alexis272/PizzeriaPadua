namespace PizzeriaPadua.API.Services
{
    public class AdminAuthService
    {
        private readonly IConfiguration _configuration;

        public AdminAuthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool ValidateCredentials(string? username, string? password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            if (string.Equals(username, Username, StringComparison.OrdinalIgnoreCase)
                && string.Equals(password, Password, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        public bool ValidateToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            try
            {
                var bytes = Convert.FromBase64String(token);
                var decoded = System.Text.Encoding.UTF8.GetString(bytes);
                var separatorIndex = decoded.IndexOf(':');
                if (separatorIndex < 0) return false;

                var username = decoded[..separatorIndex];
                var password = decoded[(separatorIndex + 1)..];
                if (ValidateCredentials(username, password))
                    return true;

                // Permitir también usuarios de personal del POS preconfigurados
                return (username.Equals("admin", StringComparison.OrdinalIgnoreCase) && password == "padua2026")
                    || (username.Equals("gerente", StringComparison.OrdinalIgnoreCase) && password == "gerente2026")
                    || (username.Equals("supervisor", StringComparison.OrdinalIgnoreCase) && password == "super2026")
                    || (username.Equals("cajero1", StringComparison.OrdinalIgnoreCase) && password == "caja2026")
                    || (username.Equals("cajero2", StringComparison.OrdinalIgnoreCase) && password == "caja2026")
                    || (username.Equals("auditor", StringComparison.OrdinalIgnoreCase) && password == "audit2026");
            }
            catch
            {
                return false;
            }
        }

        private string Username =>
            _configuration["ADMIN_USERNAME"]
            ?? _configuration["AdminCredentials:Username"]
            ?? "admin";

        private string Password =>
            _configuration["ADMIN_PASSWORD"]
            ?? _configuration["AdminCredentials:Password"]
            ?? "padua2026";
    }
}
