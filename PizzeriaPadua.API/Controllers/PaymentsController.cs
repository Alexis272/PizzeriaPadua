using System.Globalization;
using System.Text.Json;
using PizzeriaPadua.API.Data;
using PizzeriaPadua.API.Services;
using PizzeriaPadua.Core.Data;
using PizzeriaPadua.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PizzeriaPadua.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly PizzeriaPaduaContext _db;
        private readonly MercadoPagoService _mercadoPago;
        private readonly UserSessionStore _sessions;
        private readonly EventBroadcaster _broadcaster;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            PizzeriaPaduaContext db,
            MercadoPagoService mercadoPago,
            UserSessionStore sessions,
            EventBroadcaster broadcaster,
            ILogger<PaymentsController> logger)
        {
            _db = db;
            _mercadoPago = mercadoPago;
            _sessions = sessions;
            _broadcaster = broadcaster;
            _logger = logger;
        }

        public class CheckoutItemRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public string Customizations { get; set; } = "";
        }

        public class CheckoutRequest
        {
            public int UserId { get; set; }
            public string SessionToken { get; set; } = "";
            public string CustomerName { get; set; } = "";
            public List<CheckoutItemRequest> Items { get; set; } = new();
        }

        [HttpGet("mercadopago/status")]
        public IActionResult MercadoPagoStatus()
        {
            return Ok(new
            {
                configured = _mercadoPago.IsConfigured,
                commissionRate = MercadoPagoService.CommissionRate,
                vatRate = MercadoPagoService.VatRate
            });
        }

        [HttpPost("mercadopago/checkout")]
        public async Task<IActionResult> CreateMercadoPagoCheckout([FromBody] CheckoutRequest request)
        {
            if (!_mercadoPago.IsConfigured)
                return StatusCode(503, new { success = false, message = "Mercado Pago aún no está configurado" });

            if (!_sessions.Touch(request.UserId, request.SessionToken))
                return Unauthorized(new { success = false, message = "Tu sesión terminó. Inicia sesión nuevamente." });

            if (request.Items.Count == 0 || request.Items.Count > 100)
                return BadRequest(new { success = false, message = "El carrito no contiene productos válidos" });

            var user = await _db.Users.FindAsync(request.UserId);
            if (user == null || user.IsBlocked)
                return Unauthorized(new { success = false, message = "La cuenta no está disponible" });

            var requestedItems = request.Items
                .Where(item => item.ProductId > 0 && item.Quantity > 0)
                .Select(item => new CheckoutItemRequest
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Customizations = (item.Customizations ?? "").Trim()
                })
                .GroupBy(item => new { item.ProductId, item.Customizations })
                .Select(group => new CheckoutItemRequest
                {
                    ProductId = group.Key.ProductId,
                    Customizations = group.Key.Customizations,
                    Quantity = group.Sum(item => item.Quantity)
                })
                .ToList();

            var requestedProductIds = requestedItems.Select(item => item.ProductId).Distinct().ToList();
            var products = await _db.Products
                .Where(product => requestedProductIds.Contains(product.ProductId) && product.IsAvailable)
                .ToDictionaryAsync(product => product.ProductId);
            if (products.Count != requestedProductIds.Count)
                return Conflict(new { success = false, message = "Uno de los productos ya no está disponible" });

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var lines = requestedItems.Select(item =>
                {
                    var product = products[item.ProductId];
                    var delta = OrdersController.ParseCustomizationDelta(item.Customizations);
                    var unitPrice = Math.Max(0m, product.Price + delta);
                    return new OrderItem
                    {
                        ProductId = product.ProductId,
                        Product = product,
                        Quantity = item.Quantity,
                        DecimalQuantity = item.Quantity,
                        OriginalUnitPrice = product.Price,
                        UnitPrice = unitPrice,
                        Subtotal = unitPrice * item.Quantity,
                        Customizations = item.Customizations
                    };
                }).ToList();

                var fee = _mercadoPago.CalculateFee(lines.Sum(line => line.Subtotal));
                var order = new Order
                {
                    Folio = $"WEB-{DateTime.UtcNow:MMdd}-{Random.Shared.Next(1000, 9999)}",
                    UserId = user.Id,
                    Channel = "Tienda Web",
                    CustomerName = string.IsNullOrWhiteSpace(request.CustomerName) ? user.Username : request.CustomerName.Trim(),
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.PaymentPending,
                    SaleState = "Registrada",
                    SubtotalAmount = fee.Subtotal,
                    TotalAmount = fee.Total,
                    OrderItems = lines
                };

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();
                var preference = await _mercadoPago.CreatePreferenceAsync(order, user, lines, fee);
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    orderId = order.OrderId,
                    preferenceId = preference.Id,
                    initPoint = preference.InitPoint,
                    subtotal = fee.Subtotal,
                    commission = fee.Commission,
                    vat = fee.Vat,
                    fee = fee.Fee,
                    total = fee.Total
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al crear checkout de Mercado Pago");
                return StatusCode(502, new { success = false, message = "No se pudo iniciar el pago con Mercado Pago" });
            }
        }

        [HttpGet("mercadopago/confirm")]
        public async Task<IActionResult> ConfirmMercadoPago([FromQuery] string paymentId)
        {
            var result = await ProcessPaymentAsync(paymentId);
            return result == null
                ? BadRequest(new { success = false, message = "No fue posible confirmar el pago" })
                : Ok(result);
        }

        [HttpPost("mercadopago/webhook")]
        public async Task<IActionResult> MercadoPagoWebhook()
        {
            var paymentId = Request.Query["data.id"].FirstOrDefault() ?? Request.Query["id"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(paymentId))
                await ProcessPaymentAsync(paymentId);
            return Ok();
        }

        private async Task<object?> ProcessPaymentAsync(string paymentId)
        {
            var payment = await _mercadoPago.GetPaymentAsync(paymentId);
            if (payment == null || !int.TryParse(payment.ExternalReference, NumberStyles.None, CultureInfo.InvariantCulture, out var orderId))
                return null;

            var order = await _db.Orders.FindAsync(orderId);
            if (order == null) return null;

            if (payment.Status == "approved")
            {
                order.Status = OrderStatus.Pending;
                order.SaleState = "Cobrada";
                order.CompletedDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                var payload = JsonSerializer.Serialize(new { orderId = order.OrderId, userId = order.UserId, customerName = order.CustomerName, status = (int)order.Status, totalAmount = order.TotalAmount });
                await _broadcaster.NotifyAsync("order-updated", payload);
            }
            return new { success = true, approved = payment.Status == "approved", status = payment.Status, orderId };
        }
    }
}
