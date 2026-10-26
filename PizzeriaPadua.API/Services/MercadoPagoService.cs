using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PizzeriaPadua.Core.Data;

namespace PizzeriaPadua.API.Services
{
    public record PaymentFeeBreakdown(decimal Subtotal, decimal Commission, decimal Vat, decimal Fee, decimal Total);
    public record MercadoPagoPreference(string Id, string InitPoint);
    public record MercadoPagoPayment(string Id, string Status, string? ExternalReference, decimal Amount);

    public class MercadoPagoService
    {
        public const decimal CommissionRate = 0.0319m;
        public const decimal VatRate = 0.16m;

        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MercadoPagoService> _logger;

        public MercadoPagoService(HttpClient http, IConfiguration configuration, ILogger<MercadoPagoService> logger)
        {
            _http = http;
            _configuration = configuration;
            _logger = logger;
        }

        private string? AccessToken =>
            _configuration["MercadoPago:AccessToken"] ??
            _configuration["MERCADOPAGO_ACCESS_TOKEN"] ??
            _configuration["MP_ACCESS_TOKEN"];

        private string? WebhookSecret =>
            _configuration["MercadoPago:WebhookSecret"] ??
            _configuration["MERCADOPAGO_WEBHOOK_SECRET"] ??
            _configuration["MP_WEBHOOK_SECRET"];

        public bool IsConfigured => !string.IsNullOrWhiteSpace(AccessToken);

        public bool ValidateWebhookSignature(string dataId, string? requestId, string? signature)
        {
            var secret = WebhookSecret;
            if (string.IsNullOrWhiteSpace(secret)) return true;
            if (string.IsNullOrWhiteSpace(dataId) || string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(signature)) return false;

            var parts = signature.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2))
                .Where(part => part.Length == 2)
                .ToDictionary(part => part[0].Trim(), part => part[1].Trim(), StringComparer.OrdinalIgnoreCase);
            if (!parts.TryGetValue("ts", out var timestamp) || !parts.TryGetValue("v1", out var receivedHash)) return false;

            var manifest = $"id:{dataId};request-id:{requestId};ts:{timestamp};";
            var expectedHash = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();
            return CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(expectedHash),
                Encoding.ASCII.GetBytes(receivedHash.ToLowerInvariant()));
        }

        public PaymentFeeBreakdown CalculateFee(decimal subtotal)
        {
            var commission = decimal.Round(subtotal * CommissionRate, 2, MidpointRounding.AwayFromZero);
            var vat = decimal.Round(commission * VatRate, 2, MidpointRounding.AwayFromZero);
            var fee = commission + vat;
            return new PaymentFeeBreakdown(subtotal, commission, vat, fee, subtotal + fee);
        }

        public async Task<MercadoPagoPreference> CreatePreferenceAsync(
            Order order,
            User user,
            IReadOnlyCollection<OrderItem> orderItems,
            PaymentFeeBreakdown fee)
        {
            var baseUrl = (_configuration["MercadoPago:PublicBaseUrl"] ?? "http://localhost:5000/PizzeriaPadua").TrimEnd('/');
            var items = orderItems.Select(item => new
            {
                id = item.ProductId.ToString(CultureInfo.InvariantCulture),
                title = string.IsNullOrWhiteSpace(item.Customizations) ? item.Product?.Name ?? "Producto" : $"{item.Product?.Name} ({item.Customizations})",
                quantity = item.Quantity,
                currency_id = "MXN",
                unit_price = item.UnitPrice
            }).Cast<object>().ToList();

            items.Add(new
            {
                id = "processing-fee",
                title = "Cargo Mercado Pago (3.19% + IVA)",
                quantity = 1,
                currency_id = "MXN",
                unit_price = fee.Fee
            });

            var payload = new
            {
                items,
                payer = new { email = user.Email, name = user.Username },
                external_reference = order.OrderId.ToString(CultureInfo.InvariantCulture),
                notification_url = $"{baseUrl}/api/v1/Payments/mercadopago/webhook",
                back_urls = new
                {
                    success = $"{baseUrl}/?payment=success",
                    failure = $"{baseUrl}/?payment=failure",
                    pending = $"{baseUrl}/?payment=pending"
                },
                auto_return = "approved",
                statement_descriptor = "PIZZERIA PADUA"
            };

            using var request = CreateRequest(HttpMethod.Post, "checkout/preferences");
            request.Headers.TryAddWithoutValidation("X-Idempotency-Key", $"order-{order.OrderId}");
            request.Content = JsonContent.Create(payload);
            using var response = await _http.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Mercado Pago preference error {Status}: {Body}", (int)response.StatusCode, responseBody);
                throw new InvalidOperationException("Mercado Pago no pudo iniciar el cobro");
            }

            using var json = JsonDocument.Parse(responseBody);
            var root = json.RootElement;
            var id = root.GetProperty("id").GetString();
            var initPoint = root.GetProperty("init_point").GetString();
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(initPoint))
                throw new InvalidOperationException("Mercado Pago devolvió una respuesta incompleta");

            return new MercadoPagoPreference(id, initPoint);
        }

        public async Task<MercadoPagoPayment?> GetPaymentAsync(string paymentId)
        {
            if (string.IsNullOrWhiteSpace(paymentId)) return null;

            using var request = CreateRequest(HttpMethod.Get, $"v1/payments/{Uri.EscapeDataString(paymentId)}");
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = json.RootElement;
            return new MercadoPagoPayment(
                root.GetProperty("id").ToString(),
                root.GetProperty("status").GetString() ?? "unknown",
                root.TryGetProperty("external_reference", out var reference) ? reference.GetString() : null,
                root.TryGetProperty("transaction_amount", out var amount) ? amount.GetDecimal() : 0m);
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string path)
        {
            var token = AccessToken;
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Mercado Pago no está configurado");

            var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return request;
        }
    }
}
