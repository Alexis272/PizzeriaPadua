using PizzeriaPadua.API.Data;
using PizzeriaPadua.API.Services;
using PizzeriaPadua.Core.Data;
using PizzeriaPadua.Core.ViewModels.Order;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using OrderStatusEnum = PizzeriaPadua.Core.Enums.OrderStatus;

namespace PizzeriaPadua.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly PizzeriaPaduaContext _context;
        private readonly ILogger<OrdersController> _logger;
        private readonly EventBroadcaster _broadcaster;
        private readonly AdminAuthService _adminAuth;
        private readonly UserSessionStore _sessions;

        public class AdminOrderItemRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public string? Customizations { get; set; }
        }

        public class AdminOrderRequest
        {
            public string CustomerName { get; set; } = "";
            public string? Channel { get; set; }
            public int? TableId { get; set; }
            public AdminOrderItemRequest[] Items { get; set; } = Array.Empty<AdminOrderItemRequest>();
        }

        public OrdersController(PizzeriaPaduaContext context, ILogger<OrdersController> logger, EventBroadcaster broadcaster, AdminAuthService adminAuth, UserSessionStore sessions)
        {
            _context = context;
            _logger = logger;
            _broadcaster = broadcaster;
            _adminAuth = adminAuth;
            _sessions = sessions;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.Table)
                    .Include(o => o.Employee)
                    .Include(o => o.Payments)
                    .Include(o => o.Refunds)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting orders: {Error}", ex);
                return BadRequest("Internal Error");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserOrders(int userId)
        {
            if (!HasValidUserSession(userId))
                return Unauthorized(new { success = false, message = "Sesión de usuario requerida" });

            try
            {
                var orders = await _context.Orders
                    .Where(o => o.UserId == userId)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting user orders: {Error}", ex);
                return BadRequest("Internal Error");
            }
        }

        [HttpGet("recent-completed")]
        public async Task<IActionResult> GetRecentCompleted()
        {
            try
            {
                var fiveMinAgo = DateTime.UtcNow.AddMinutes(-2);
                var orders = await _context.Orders
                    .Where(o => o.Status == OrderStatusEnum.ReadyForDelivery && o.CompletedDate >= fiveMinAgo)
                    .OrderByDescending(o => o.CompletedDate)
                    .Take(15)
                    .Select(o => new
                    {
                        o.OrderId,
                        o.Folio,
                        o.CustomerName,
                        Status = (int)o.Status,
                        o.TotalAmount,
                        o.OrderDate,
                        CompletedDate = o.CompletedDate
                    })
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting recent completed orders: {Error}", ex);
                return BadRequest("Internal Error");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Table)
                    .Include(o => o.Employee)
                    .Include(o => o.Payments)
                    .Include(o => o.Refunds)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                    return NotFound();

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting order: {Error}", ex);
                return BadRequest("Internal Error");
            }
        }

        private async Task<string> NextFolioAsync(int cashRegisterId = 1)
        {
            var reg = await _context.CashRegisters.FindAsync(cashRegisterId);
            if (reg != null)
            {
                reg.CurrentFolio += 1;
                return $"{reg.SeriesPrefix}-{reg.CurrentFolio:D6}";
            }
            var count = await _context.Orders.CountAsync() + 101;
            return $"PAD-C1-{count:D6}";
        }

        [HttpPut("add")]
        public async Task<IActionResult> AddOrder([FromBody] AddOrderView model)
        {
            try
            {
                var activeSession = await _context.CashSessions.FirstOrDefaultAsync(s => s.Status == "Abierta");
                var folio = await NextFolioAsync(activeSession?.CashRegisterId ?? 1);

                var order = new Order
                {
                    Folio = folio,
                    BranchId = activeSession?.BranchId ?? 1,
                    CashRegisterId = activeSession?.CashRegisterId ?? 1,
                    CashSessionId = activeSession?.CashSessionId,
                    ShiftName = activeSession?.ShiftName ?? "Matutino",
                    UserId = model.UserId,
                    TableId = model.TableId,
                    EmployeeId = model.EmployeeId,
                    CustomerName = model.CustomerName,
                    Channel = string.IsNullOrWhiteSpace(model.Channel) ? "Tienda Web" : model.Channel,
                    Notes = model.Notes,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatusEnum.Pending,
                    SaleState = "Registrada",
                    TotalAmount = 0
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                NotifyOrderChanged(order);

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error adding order: {Error}", ex);
                return BadRequest("Internal Error");
            }
        }

        [HttpPost("admin/add")]
        public async Task<IActionResult> AddAdminOrder([FromBody] AdminOrderRequest model)
        {
            try
            {
                var customerName = (model.CustomerName ?? "").Trim();
                if (string.IsNullOrWhiteSpace(customerName))
                    return BadRequest(new { success = false, message = "Ingresa el nombre del cliente" });

                var items = model.Items?
                    .Where(i => i.ProductId > 0 && i.Quantity > 0)
                    .Select(i => new AdminOrderItemRequest
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        Customizations = (i.Customizations ?? "").Trim()
                    })
                    .GroupBy(i => new { i.ProductId, i.Customizations })
                    .Select(g => new AdminOrderItemRequest
                    {
                        ProductId = g.Key.ProductId,
                        Customizations = g.Key.Customizations,
                        Quantity = g.Sum(i => i.Quantity)
                    })
                    .ToList() ?? new List<AdminOrderItemRequest>();

                if (items.Count == 0)
                    return BadRequest(new { success = false, message = "Agrega al menos un producto al pedido" });

                var productIds = items.Select(i => i.ProductId).Distinct().ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToDictionaryAsync(p => p.ProductId);

                foreach (var item in items)
                {
                    if (!products.TryGetValue(item.ProductId, out var product))
                        return BadRequest(new { success = false, message = "Uno de los productos ya no existe" });

                    if (!product.IsAvailable)
                        return BadRequest(new { success = false, message = $"El producto {product.Name} no está disponible" });
                }

                var activeSession = await _context.CashSessions.FirstOrDefaultAsync(s => s.Status == "Abierta");
                var folio = await NextFolioAsync(activeSession?.CashRegisterId ?? 1);

                var order = new Order
                {
                    Folio = folio,
                    BranchId = activeSession?.BranchId ?? 1,
                    CashRegisterId = activeSession?.CashRegisterId ?? 1,
                    CashSessionId = activeSession?.CashSessionId,
                    ShiftName = activeSession?.ShiftName ?? "Matutino",
                    CashierName = activeSession?.OpenedByUsername ?? "admin",
                    CustomerName = customerName,
                    Channel = string.IsNullOrWhiteSpace(model.Channel) ? "Mostrador" : model.Channel,
                    TableId = model.TableId,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatusEnum.Pending,
                    SaleState = "Registrada",
                    TotalAmount = 0,
                    OrderItems = new List<OrderItem>()
                };

                foreach (var item in items)
                {
                    var product = products[item.ProductId];
                    var extraPrice = ParseCustomizationDelta(item.Customizations);
                    var unitPrice = Math.Max(0m, product.Price + extraPrice);
                    var subtotal = unitPrice * item.Quantity;
                    var tax = decimal.Round(subtotal - (subtotal / (1m + product.TaxRate)), 2);

                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = product.ProductId,
                        Quantity = item.Quantity,
                        DecimalQuantity = item.Quantity,
                        OriginalUnitPrice = product.Price,
                        UnitPrice = unitPrice,
                        TaxAmount = tax,
                        Subtotal = subtotal,
                        Customizations = item.Customizations
                    });
                    order.SubtotalAmount += subtotal;
                    order.TaxAmount += tax;
                    order.TotalAmount += subtotal;
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                NotifyOrderChanged(order);

                return Ok(new { success = true, orderId = order.OrderId, folio = order.Folio });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error adding admin order: {Error}", ex);
                return BadRequest(new { success = false, message = "No se pudo crear el pedido" });
            }
        }

        public static decimal ParseCustomizationDelta(string? customizations)
        {
            if (string.IsNullOrWhiteSpace(customizations)) return 0m;
            decimal delta = 0m;
            var matches = System.Text.RegularExpressions.Regex.Matches(customizations, @"\(([+-]?)\$(\d+(?:\.\d+)?)\)");
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                if (decimal.TryParse(m.Groups[2].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
                {
                    delta += m.Groups[1].Value == "-" ? -val : val;
                }
            }
            return delta;
        }

        [HttpGet("active/{userId}")]
        public async Task<IActionResult> GetActiveOrders(int userId)
        {
            if (!HasValidUserSession(userId))
                return Unauthorized(new { success = false, message = "Sesión de usuario requerida" });

            try
            {
                var orders = await _context.Orders
                    .Where(o => o.UserId == userId
                        && (o.Status == OrderStatusEnum.Pending || o.Status == OrderStatusEnum.InProgress))
                    .OrderBy(o => o.OrderDate)
                    .Select(o => new
                    {
                        o.OrderId,
                        o.Folio,
                        o.CustomerName,
                        Status = (int)o.Status,
                        o.TotalAmount,
                        o.OrderDate
                    })
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting active orders: {Error}", ex);
                return BadRequest("Internal Error");
            }
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateOrder([FromBody] UpdateOrderView model)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.OrderId == model.OrderId);

                if (order == null)
                    return NotFound();

                order.TableId = model.TableId ?? order.TableId;
                order.EmployeeId = model.EmployeeId ?? order.EmployeeId;
                order.Status = (OrderStatusEnum)model.Status;
                order.ModifiedAt = DateTime.UtcNow;

                if (order.Status == OrderStatusEnum.Completed)
                {
                    order.CompletedDate = DateTime.UtcNow;
                    order.SaleState = "Cobrada";
                    order.IsOpenAccount = false;

                    if (order.Payments == null || order.Payments.Count == 0)
                    {
                        var activeSession = await _context.CashSessions.FirstOrDefaultAsync(s => s.Status == "Abierta");
                        order.CashSessionId ??= activeSession?.CashSessionId;
                        _context.SalePayments.Add(new SalePayment
                        {
                            OrderId = order.OrderId,
                            CashSessionId = order.CashSessionId,
                            BranchId = order.BranchId,
                            CashRegisterId = order.CashRegisterId,
                            PaymentMethod = string.IsNullOrWhiteSpace(order.PaymentMethodSummary) ? "Efectivo" : order.PaymentMethodSummary,
                            Amount = order.TotalAmount,
                            AmountReceived = order.AmountTendered > 0 ? order.AmountTendered : order.TotalAmount,
                            ChangeGiven = order.ChangeGiven,
                            CashierUsername = order.CashierName,
                            Status = "Aprobado",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                else if (order.Status == OrderStatusEnum.ReadyForDelivery)
                {
                    order.CompletedDate = DateTime.UtcNow;
                }
                else if (order.Status == OrderStatusEnum.Cancelled)
                {
                    order.SaleState = "CanceladaTotal";
                    order.CancelledAt = DateTime.UtcNow;
                }

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                NotifyOrderChanged(order);
                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error updating order: {Error}", ex);
                return BadRequest("Internal Error");
            }
        }

        [HttpPost("{id:int}/notify")]
        public async Task<IActionResult> NotifyReadyOrder(int id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                    return NotFound(new { success = false, message = "Pedido no encontrado" });

                if (order.Status != OrderStatusEnum.ReadyForDelivery)
                    return BadRequest(new { success = false, message = "El pedido no está pendiente de entrega" });

                order.CompletedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                NotifyOrderChanged(order);

                return Ok(new { success = true, message = "Cliente notificado nuevamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error notifying ready order: {Error}", ex);
                return BadRequest(new { success = false, message = "No se pudo volver a notificar" });
            }
        }

        [HttpDelete("delete/{id:int}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                    return NotFound();

                // Requerimiento 14 & 16 (Regla 31): No borrar físicamente ventas, usar estado cancelado y conservar folio
                order.Status = OrderStatusEnum.Cancelled;
                order.SaleState = "CanceladaTotal";
                order.CancellationReason ??= "Cancelada antes del cobro en mostrador";
                order.CancelledBy ??= "cajero1";
                order.CancelledAt = DateTime.UtcNow;
                order.IsOpenAccount = false;

                if (order.TableId.HasValue)
                {
                    var tbl = await _context.Tables.FindAsync(order.TableId.Value);
                    if (tbl != null)
                    {
                        tbl.IsAvailable = true;
                        tbl.CurrentOrderId = null;
                    }
                }

                _context.AuditLogs.Add(new AuditLog
                {
                    Operation = "CANCELAR_VENTA",
                    EntityName = "Order",
                    EntityId = order.OrderId.ToString(),
                    Folio = order.Folio,
                    PreviousValue = "Activa",
                    NewValue = "CanceladaTotal",
                    Reason = order.CancellationReason,
                    Username = order.CancelledBy ?? "cajero1",
                    BranchId = order.BranchId,
                    CashRegisterId = order.CashRegisterId,
                    Result = "Exitoso"
                });

                await _context.SaveChangesAsync();
                NotifyOrderChanged(order);

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error cancelling order: {Error}", ex);
                return BadRequest("Internal Error");
            }
        }

        [HttpDelete("history")]
        public async Task<IActionResult> DeleteHistory()
        {
            return BadRequest(new
            {
                success = false,
                message = "Por normativa de auditoría de Pizzería Padua (Sección 14), las ventas y cortes no pueden borrarse físicamente; utilice estados de cancelación o devolución."
            });
        }

        [HttpPost("add-item")]
        public async Task<IActionResult> AddOrderItem([FromBody] AddOrderItemView model)
        {
            try
            {
                var order = await _context.Orders.FindAsync(model.OrderId);
                var product = await _context.Products.FindAsync(model.ProductId);

                if (order == null || product == null)
                    return NotFound("Order or Product not found");

                var extraPrice = ParseCustomizationDelta(model.Customizations);
                var unitPrice = model.OverrideUnitPrice ?? Math.Max(0m, product.Price + extraPrice);
                var subtotal = unitPrice * model.Quantity;
                var tax = decimal.Round(subtotal - (subtotal / (1m + product.TaxRate)), 2);

                var orderItem = new OrderItem
                {
                    OrderId = model.OrderId,
                    ProductId = model.ProductId,
                    Quantity = model.Quantity,
                    DecimalQuantity = model.Quantity,
                    OriginalUnitPrice = product.Price,
                    UnitPrice = unitPrice,
                    TaxAmount = tax,
                    Subtotal = subtotal,
                    Customizations = model.Customizations
                };

                _context.OrderItems.Add(orderItem);

                order.SubtotalAmount += subtotal;
                order.TaxAmount += tax;
                order.TotalAmount += subtotal;

                await _context.SaveChangesAsync();
                NotifyOrderChanged(order);

                return Ok(orderItem);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error adding order item: {Error}", ex);
                return BadRequest("Internal Error");
            }
        }

        private void NotifyOrderChanged(Order order)
        {
            var payload = JsonSerializer.Serialize(new
            {
                orderId = order.OrderId,
                folio = order.Folio,
                userId = order.UserId,
                customerName = order.CustomerName,
                status = (int)order.Status,
                saleState = order.SaleState,
                totalAmount = order.TotalAmount
            });
            _ = _broadcaster.NotifyAsync("order-updated", payload);
        }

        private bool HasValidUserSession(int userId)
        {
            return Request.Headers.TryGetValue("X-Session-Token", out var token)
                && _sessions.IsValid(userId, token.ToString());
        }
    }
}
