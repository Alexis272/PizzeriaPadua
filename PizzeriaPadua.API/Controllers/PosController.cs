using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaPadua.API.Data;
using PizzeriaPadua.API.Services;
using PizzeriaPadua.Core.Data;
using PizzeriaPadua.Core.Enums;
using System.Text.Json;

namespace PizzeriaPadua.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PosController : ControllerBase
    {
        private readonly PizzeriaPaduaContext _db;
        private readonly EventBroadcaster _broadcaster;
        private readonly ILogger<PosController> _logger;

        public PosController(PizzeriaPaduaContext db, EventBroadcaster broadcaster, ILogger<PosController> logger)
        {
            _db = db;
            _broadcaster = broadcaster;
            _logger = logger;
        }

        // ==================== DTOs ====================
        public class AuthorizeRequest
        {
            public string PinOrPassword { get; set; } = "";
            public string? Username { get; set; }
            public string Operation { get; set; } = "";
            public string? Reason { get; set; }
        }

        public class OpenCashRequest
        {
            public int BranchId { get; set; } = 1;
            public int CashRegisterId { get; set; } = 1;
            public string ShiftName { get; set; } = "Matutino";
            public string CashierUsername { get; set; } = "cajero1";
            public decimal InitialFund { get; set; }
            public string? DenominationsJson { get; set; }
            public string? OpeningNotes { get; set; }
            public string? AuthorizerPin { get; set; }
            public bool ForceReopen { get; set; } = false;
        }

        public class CashMovementRequest
        {
            public int? CashSessionId { get; set; }
            public int BranchId { get; set; } = 1;
            public int CashRegisterId { get; set; } = 1;
            public string MovementType { get; set; } = "EntradaExtraordinaria";
            public string PaymentMethod { get; set; } = "Efectivo";
            public decimal Amount { get; set; }
            public string Reason { get; set; } = "";
            public string? Notes { get; set; }
            public string Username { get; set; } = "cajero1";
            public string? AuthorizerPin { get; set; }
        }

        public class CashCutRequest
        {
            public string CutType { get; set; } = "Parcial"; // Parcial | Final
            public int? CashSessionId { get; set; }
            public int BranchId { get; set; } = 1;
            public int CashRegisterId { get; set; } = 1;
            public decimal CountedCash { get; set; }
            public decimal CardCounted { get; set; }
            public decimal TransferCounted { get; set; }
            public decimal OtherCounted { get; set; }
            public string? DenominationsJson { get; set; }
            public string? DifferenceExplanation { get; set; }
            public string? Notes { get; set; }
            public string ResponsibleUsername { get; set; } = "cajero1";
            public string? SupervisorPin { get; set; }
            public bool AllowOpenAccountsOverride { get; set; } = false;
        }

        public class PosSaleItemRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; } = 1;
            public decimal DecimalQuantity { get; set; } = 1m;
            public decimal? CustomUnitPrice { get; set; }
            public string? PriceOverrideReason { get; set; }
            public decimal ItemDiscount { get; set; } = 0m;
            public string? Customizations { get; set; }
            public string? Notes { get; set; }
        }

        public class PosPaymentLineRequest
        {
            public string PaymentMethod { get; set; } = "Efectivo";
            public decimal Amount { get; set; }
            public decimal TipAmount { get; set; } = 0m;
            public decimal AmountReceived { get; set; }
            public string? Reference { get; set; }
            public string Status { get; set; } = "Aprobado";
        }

        public class PosSaleRequest
        {
            public int BranchId { get; set; } = 1;
            public int CashRegisterId { get; set; } = 1;
            public int? CashSessionId { get; set; }
            public string CashierUsername { get; set; } = "cajero1";
            public string? SellerName { get; set; }
            public string Channel { get; set; } = "Mostrador";
            public string SaleType { get; set; } = "Contado";
            public string CustomerType { get; set; } = "General";
            public string? CustomerName { get; set; }
            public string? CustomerPhone { get; set; }
            public string? CustomerRfc { get; set; }
            public bool RequiresInvoice { get; set; } = false;
            public int? TableId { get; set; }
            public bool SaveAsOpenAccount { get; set; } = false;
            public decimal DiscountPercent { get; set; } = 0m;
            public decimal DiscountAmount { get; set; } = 0m;
            public string? PromoCode { get; set; }
            public string? DiscountReason { get; set; }
            public decimal TipAmount { get; set; } = 0m;
            public string? Notes { get; set; }
            public string? AuthorizerPin { get; set; }
            public List<PosSaleItemRequest> Items { get; set; } = new();
            public List<PosPaymentLineRequest> Payments { get; set; } = new();
        }

        public class SettleAccountRequest
        {
            public string CashierUsername { get; set; } = "cajero1";
            public decimal DiscountPercent { get; set; } = 0m;
            public decimal DiscountAmount { get; set; } = 0m;
            public string? PromoCode { get; set; }
            public string? DiscountReason { get; set; }
            public decimal TipAmount { get; set; } = 0m;
            public string? AuthorizerPin { get; set; }
            public List<PosPaymentLineRequest> Payments { get; set; } = new();
        }

        public class ModifyAccountRequest
        {
            public string Username { get; set; } = "cajero1";
            public string? AuthorizerPin { get; set; }
            public string? Reason { get; set; }
            public List<PosSaleItemRequest> AddItems { get; set; } = new();
            public List<int> RemoveOrderItemIds { get; set; } = new();
        }

        public class TransferAccountRequest
        {
            public int? NewTableId { get; set; }
            public string? NewCustomerName { get; set; }
            public string? NewSellerName { get; set; }
            public string Username { get; set; } = "cajero1";
            public string? AuthorizerPin { get; set; }
            public string Reason { get; set; } = "Transferencia de mesa solicitada por cliente";
        }

        public class CancelSaleRequest
        {
            public string Reason { get; set; } = "";
            public string Username { get; set; } = "cajero1";
            public string? AuthorizerPin { get; set; }
        }

        public class RefundItemDto
        {
            public int OrderItemId { get; set; }
            public int ProductId { get; set; }
            public string ProductName { get; set; } = "";
            public int Quantity { get; set; }
            public decimal Amount { get; set; }
        }

        public class RefundSaleRequest
        {
            public string RefundType { get; set; } = "Parcial"; // Total | Parcial
            public decimal RefundAmount { get; set; }
            public string RefundMethod { get; set; } = "Efectivo";
            public string Reason { get; set; } = "";
            public string InventoryDisposition { get; set; } = "Merma"; // Reincorporar | Merma | Revision
            public string Username { get; set; } = "cajero1";
            public string? AuthorizerPin { get; set; }
            public List<RefundItemDto> Items { get; set; } = new();
        }

        // ==================== HELPERS ====================
        private async Task<Employee?> ValidateAuthorizerAsync(string? pinOrPass, string? username = null)
        {
            if (string.IsNullOrWhiteSpace(pinOrPass)) return null;
            var clean = pinOrPass.Trim();

            var query = _db.Employees.Where(e => e.IsActive &&
                (e.Role == "Administrador" || e.Role == "Gerente" || e.Role == "Supervisor"));

            if (!string.IsNullOrWhiteSpace(username))
            {
                var u = username.Trim().ToLower();
                return await query.FirstOrDefaultAsync(e => e.Username.ToLower() == u && (e.PinCode == clean || e.Password == clean));
            }

            return await query.FirstOrDefaultAsync(e => e.PinCode == clean || e.Password == clean);
        }

        private async Task<(decimal InitialFund, decimal CashSales, decimal CashInflows, decimal CashOutflows, decimal CashRefunds, decimal AuthorizedOutflows, decimal ExpectedCash, decimal CardExpected, decimal TransferExpected, decimal OtherExpected)> CalculateSessionExpectedAsync(CashSession session)
        {
            var orders = await _db.Orders
                .Include(o => o.Payments)
                .Where(o => o.CashSessionId == session.CashSessionId && o.Status != OrderStatus.Cancelled && !o.IsOpenAccount)
                .AsNoTracking()
                .ToListAsync();

            var payments = orders.SelectMany(o => o.Payments ?? new List<SalePayment>())
                .Where(p => p.Status == "Aprobado")
                .ToList();

            decimal cashSales = payments
                .Where(p => p.PaymentMethod.Contains("Efectivo", StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.Amount);

            decimal cardExpected = payments
                .Where(p => p.PaymentMethod.Contains("Tarjeta", StringComparison.OrdinalIgnoreCase) || p.PaymentMethod.Contains("TDC", StringComparison.OrdinalIgnoreCase) || p.PaymentMethod.Contains("TDB", StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.Amount);

            decimal transferExpected = payments
                .Where(p => p.PaymentMethod.Contains("Transferencia", StringComparison.OrdinalIgnoreCase) || p.PaymentMethod.Contains("SPEI", StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.Amount);

            decimal otherExpected = payments
                .Where(p => !p.PaymentMethod.Contains("Efectivo", StringComparison.OrdinalIgnoreCase)
                         && !p.PaymentMethod.Contains("Tarjeta", StringComparison.OrdinalIgnoreCase)
                         && !p.PaymentMethod.Contains("TDC", StringComparison.OrdinalIgnoreCase)
                         && !p.PaymentMethod.Contains("TDB", StringComparison.OrdinalIgnoreCase)
                         && !p.PaymentMethod.Contains("Transferencia", StringComparison.OrdinalIgnoreCase)
                         && !p.PaymentMethod.Contains("SPEI", StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.Amount);

            var movements = await _db.CashMovements
                .Where(m => m.CashSessionId == session.CashSessionId && m.MovementType != "FondoInicial")
                .AsNoTracking()
                .ToListAsync();

            decimal cashInflows = movements
                .Where(m => m.Direction == "Entrada" && m.PaymentMethod.Contains("Efectivo", StringComparison.OrdinalIgnoreCase))
                .Sum(m => m.Amount);

            decimal cashOutflows = movements
                .Where(m => m.Direction == "Salida" && (m.MovementType == "RetiroParcial" || m.MovementType == "Deposito"))
                .Sum(m => m.Amount);

            decimal cashRefunds = movements
                .Where(m => m.Direction == "Salida" && m.MovementType == "ReembolsoEfectivo")
                .Sum(m => m.Amount);

            decimal authorizedOutflows = movements
                .Where(m => m.Direction == "Salida" && m.MovementType != "RetiroParcial" && m.MovementType != "Deposito" && m.MovementType != "ReembolsoEfectivo")
                .Sum(m => m.Amount);

            // Sección 10.3 Fórmula mínima de efectivo esperado:
            // Efectivo esperado = fondo inicial + ventas cobradas en efectivo + entradas de efectivo - retiros de efectivo - reembolsos en efectivo - salidas autorizadas
            decimal expectedCash = session.InitialFund + cashSales + cashInflows - cashOutflows - cashRefunds - authorizedOutflows;

            return (session.InitialFund, cashSales, cashInflows, cashOutflows, cashRefunds, authorizedOutflows, expectedCash, cardExpected, transferExpected, otherExpected);
        }

        // ==================== ENDPOINTS ====================

        [HttpGet("bootstrap")]
        public async Task<IActionResult> Bootstrap()
        {
            var products = await _db.Products.Include(p => p.Category).AsNoTracking().ToListAsync();
            var branches = await _db.Branches.AsNoTracking().ToListAsync();
            var registers = await _db.CashRegisters.AsNoTracking().ToListAsync();
            var shifts = await _db.ShiftConfigs.AsNoTracking().ToListAsync();
            var activeSessions = await _db.CashSessions.Where(s => s.Status == "Abierta").OrderByDescending(s => s.OpenedAt).AsNoTracking().ToListAsync();
            var employees = await _db.Employees.Where(e => e.IsActive).Select(e => new
            {
                e.EmployeeId,
                e.Name,
                e.LastName,
                e.Username,
                e.Role,
                e.Position,
                e.BranchId,
                e.AllowedCashRegisterId,
                e.CanModifyPrice,
                e.CanAuthorizeDiscount,
                e.CanCancelPaidSale,
                e.CanProcessRefund,
                e.CanManageCatalog,
                e.CanOpenCloseCash,
                e.CanRegisterCashMovement,
                e.CanAdjustCashDifference,
                e.CanViewFinancialReports,
                e.CanExportReports,
                e.MaxManualDiscountPercent
            }).AsNoTracking().ToListAsync();
            var paymentMethods = await _db.PaymentMethodConfigs.Where(p => p.IsActive).AsNoTracking().ToListAsync();
            var channels = await _db.SalesChannelConfigs.Where(c => c.IsActive).AsNoTracking().ToListAsync();
            var reasons = await _db.CatalogReasons.Where(r => r.IsActive).AsNoTracking().ToListAsync();
            var promotions = await _db.Promotions.AsNoTracking().ToListAsync();
            var goals = await _db.SalesGoals.AsNoTracking().ToListAsync();
            var tables = await _db.Tables.AsNoTracking().ToListAsync();
            var settings = await _db.StoreSettings.FirstOrDefaultAsync();

            return Ok(new
            {
                branches,
                registers,
                shifts,
                activeSessions,
                employees,
                paymentMethods,
                channels,
                reasons,
                promotions,
                goals,
                tables,
                settings,
                products
            });
        }

        [HttpPost("authorize")]
        public async Task<IActionResult> Authorize([FromBody] AuthorizeRequest request)
        {
            var auth = await ValidateAuthorizerAsync(request.PinOrPassword, request.Username);
            if (auth == null)
            {
                _db.AuditLogs.Add(new AuditLog
                {
                    Operation = string.IsNullOrWhiteSpace(request.Operation) ? "AUTORIZACION_FALLIDA" : request.Operation,
                    EntityName = "Authorization",
                    Reason = request.Reason ?? "PIN o contraseña de supervisor inválido",
                    Username = request.Username ?? "cajero",
                    Result = "Rechazado"
                });
                await _db.SaveChangesAsync();
                return Unauthorized(new { success = false, message = "PIN o credenciales de autorizador inválidas (Se requiere Supervisor, Gerente o Administrador)" });
            }

            return Ok(new
            {
                success = true,
                authorizedBy = $"{auth.Username} ({auth.Role})",
                authorizerName = $"{auth.Name} {auth.LastName}".Trim(),
                role = auth.Role
            });
        }

        // 5.1 Inicio de turno y arranque de caja
        [HttpPost("cash/open")]
        public async Task<IActionResult> OpenCash([FromBody] OpenCashRequest request)
        {
            var register = await _db.CashRegisters.FindAsync(request.CashRegisterId);
            if (register == null)
                return NotFound(new { success = false, message = "Caja no encontrada" });

            var existingOpen = await _db.CashSessions
                .FirstOrDefaultAsync(s => s.CashRegisterId == request.CashRegisterId && s.Status == "Abierta");

            string? authorizedBy = null;

            // Regla 12: Impedir una segunda apertura de la misma caja cuando exista una caja abierta, salvo autorización.
            if (existingOpen != null)
            {
                if (!request.ForceReopen)
                    return Conflict(new { success = false, requiresAuth = true, message = $"La caja {register.Name} ya tiene un turno abierto (Folio {existingOpen.Folio} por {existingOpen.OpenedByUsername}). Para forzar nueva apertura se requiere autorización de supervisor." });

                var auth = await ValidateAuthorizerAsync(request.AuthorizerPin);
                if (auth == null)
                    return Unauthorized(new { success = false, message = "Se requiere PIN válido de Supervisor/Gerente para autorizar apertura sobre caja abierta." });

                authorizedBy = $"{auth.Username} ({auth.Role})";
                existingOpen.Status = "Cerrada";
                existingOpen.ClosedAt = DateTime.UtcNow;
                existingOpen.ClosedByUsername = request.CashierUsername;
                existingOpen.CloseAuthorizedBy = authorizedBy;
            }

            // Regla 14: Permitir apertura con fondo cero cuando la operación lo requiera, dejando registro.
            if (request.InitialFund <= 0)
            {
                var auth = await ValidateAuthorizerAsync(request.AuthorizerPin);
                authorizedBy = auth != null ? $"{auth.Username} ({auth.Role})" : "Apertura Fondo Cero Registrada";
            }

            var sessionCount = await _db.CashSessions.CountAsync() + 1;
            var folio = $"ARR-S{request.BranchId}-C{request.CashRegisterId}-{sessionCount:D4}";

            var session = new CashSession
            {
                Folio = folio,
                BranchId = request.BranchId,
                CashRegisterId = request.CashRegisterId,
                CashRegisterName = register.Name,
                ShiftName = string.IsNullOrWhiteSpace(request.ShiftName) ? "Matutino" : request.ShiftName,
                OpenedByUsername = string.IsNullOrWhiteSpace(request.CashierUsername) ? "cajero1" : request.CashierUsername,
                AuthorizedByUsername = authorizedBy,
                OpenedAt = DateTime.UtcNow,
                InitialFund = request.InitialFund,
                DenominationsJson = request.DenominationsJson,
                OpeningNotes = request.OpeningNotes,
                Status = "Abierta",
                ExpectedCash = request.InitialFund
            };

            _db.CashSessions.Add(session);
            await _db.SaveChangesAsync();

            register.Status = "Abierta";
            register.ActiveSessionId = session.CashSessionId;

            var movCount = await _db.CashMovements.CountAsync() + 1;
            _db.CashMovements.Add(new CashMovement
            {
                Folio = $"MOV-C{request.CashRegisterId}-{movCount:D4}",
                CashSessionId = session.CashSessionId,
                BranchId = request.BranchId,
                CashRegisterId = request.CashRegisterId,
                MovementType = "FondoInicial",
                Direction = "Entrada",
                PaymentMethod = "Efectivo",
                Amount = request.InitialFund,
                Reason = request.InitialFund == 0 ? "Arranque de caja con fondo cero" : "Fondo inicial de arranque de caja",
                Notes = request.OpeningNotes,
                CreatedByUsername = session.OpenedByUsername,
                AuthorizedByUsername = authorizedBy,
                CreatedAt = DateTime.UtcNow
            });

            _db.AuditLogs.Add(new AuditLog
            {
                Operation = request.InitialFund == 0 ? "ARRANQUE_CAJA_FONDO_CERO" : "ARRANQUE_CAJA",
                EntityName = "CashSession",
                EntityId = session.CashSessionId.ToString(),
                Folio = session.Folio,
                PreviousValue = "Cerrada",
                NewValue = $"Abierta con fondo inicial ${request.InitialFund:F2} MXN ({session.ShiftName})",
                Reason = string.IsNullOrWhiteSpace(request.OpeningNotes) ? "Inicio de turno" : request.OpeningNotes,
                Username = session.OpenedByUsername,
                AuthorizedBy = authorizedBy,
                BranchId = request.BranchId,
                CashRegisterId = request.CashRegisterId,
                Result = "Exitoso"
            });

            await _db.SaveChangesAsync();
            await _broadcaster.NotifyAsync("cash-updated", JsonSerializer.Serialize(new { session.CashSessionId, session.Folio, session.Status }));

            return Ok(new { success = true, session });
        }

        // 8. Movimientos de caja
        [HttpPost("cash/movement")]
        public async Task<IActionResult> RegisterCashMovement([FromBody] CashMovementRequest request)
        {
            if (request.Amount <= 0)
                return BadRequest(new { success = false, message = "El importe del movimiento debe ser mayor a $0.00" });

            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { success = false, message = "El motivo del movimiento es obligatorio" });

            var session = request.CashSessionId.HasValue
                ? await _db.CashSessions.FindAsync(request.CashSessionId.Value)
                : await _db.CashSessions.FirstOrDefaultAsync(s => s.CashRegisterId == request.CashRegisterId && s.Status == "Abierta");

            if (session == null || session.Status != "Abierta")
                return BadRequest(new { success = false, message = "No hay una sesión de caja abierta para registrar este movimiento." });

            // Permiso sensible 3.2: Registrar entradas o retiros extraordinarios o ajustar diferencia
            var auth = await ValidateAuthorizerAsync(request.AuthorizerPin);
            if (auth == null)
                return Unauthorized(new { success = false, message = "Los movimientos de caja requieren PIN de autorización de Supervisor, Gerente o Administrador." });

            var isInflow = request.MovementType is "EntradaExtraordinaria" or "FondoInicial"
                        || (request.MovementType == "AjusteAutorizado" && request.Amount > 0 && (request.Notes ?? "").Contains("positivo", StringComparison.OrdinalIgnoreCase));

            var direction = request.MovementType is "RetiroParcial" or "Deposito" or "PagoAutorizado" or "SalidaCambio" or "ReembolsoEfectivo"
                ? "Salida"
                : isInflow ? "Entrada" : "Salida";

            var count = await _db.CashMovements.CountAsync() + 1;
            var movement = new CashMovement
            {
                Folio = $"MOV-C{session.CashRegisterId}-{count:D4}",
                CashSessionId = session.CashSessionId,
                BranchId = session.BranchId,
                CashRegisterId = session.CashRegisterId,
                MovementType = request.MovementType,
                Direction = direction,
                PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Efectivo" : request.PaymentMethod,
                Amount = Math.Abs(request.Amount),
                Reason = request.Reason.Trim(),
                Notes = request.Notes,
                CreatedByUsername = request.Username,
                AuthorizedByUsername = $"{auth.Username} ({auth.Role})",
                CreatedAt = DateTime.UtcNow
            };

            _db.CashMovements.Add(movement);
            _db.AuditLogs.Add(new AuditLog
            {
                Operation = $"MOVIMIENTO_CAJA_{request.MovementType.ToUpper()}",
                EntityName = "CashMovement",
                Folio = movement.Folio,
                PreviousValue = $"Caja #{session.CashRegisterId}",
                NewValue = $"{direction}: ${movement.Amount:F2} ({movement.PaymentMethod})",
                Reason = movement.Reason,
                Username = request.Username,
                AuthorizedBy = movement.AuthorizedByUsername,
                BranchId = session.BranchId,
                CashRegisterId = session.CashRegisterId,
                Result = "Exitoso"
            });

            await _db.SaveChangesAsync();
            var calc = await CalculateSessionExpectedAsync(session);
            session.ExpectedCash = calc.ExpectedCash;
            await _db.SaveChangesAsync();

            await _broadcaster.NotifyAsync("cash-updated", JsonSerializer.Serialize(new { movement.Folio, session.ExpectedCash }));
            return Ok(new { success = true, movement, expectedCash = calc.ExpectedCash });
        }

        [HttpGet("cash/expected")]
        public async Task<IActionResult> GetExpectedCash([FromQuery] int cashRegisterId = 1, [FromQuery] int? cashSessionId = null)
        {
            var session = cashSessionId.HasValue
                ? await _db.CashSessions.FindAsync(cashSessionId.Value)
                : await _db.CashSessions.FirstOrDefaultAsync(s => s.CashRegisterId == cashRegisterId && s.Status == "Abierta");

            if (session == null)
                return Ok(new { hasOpenSession = false });

            var calc = await CalculateSessionExpectedAsync(session);
            var openAccountsCount = await _db.Orders.CountAsync(o => o.BranchId == session.BranchId && o.IsOpenAccount && o.Status == OrderStatus.OpenAccount);

            return Ok(new
            {
                hasOpenSession = true,
                session,
                initialFund = calc.InitialFund,
                cashSales = calc.CashSales,
                cashInflows = calc.CashInflows,
                cashOutflows = calc.CashOutflows,
                cashRefunds = calc.CashRefunds,
                authorizedOutflows = calc.AuthorizedOutflows,
                expectedCash = calc.ExpectedCash,
                cardExpected = calc.CardExpected,
                transferExpected = calc.TransferExpected,
                otherExpected = calc.OtherExpected,
                totalExpected = calc.ExpectedCash + calc.CardExpected + calc.TransferExpected + calc.OtherExpected,
                openAccountsCount
            });
        }

        // 10. Corte parcial (Arqueo) y Corte final (Cierre)
        [HttpPost("cash/cut")]
        public async Task<IActionResult> ExecuteCashCut([FromBody] CashCutRequest request)
        {
            var session = request.CashSessionId.HasValue
                ? await _db.CashSessions.FindAsync(request.CashSessionId.Value)
                : await _db.CashSessions.FirstOrDefaultAsync(s => s.CashRegisterId == request.CashRegisterId && s.Status == "Abierta");

            if (session == null || session.Status == "Cerrada")
                return BadRequest(new { success = false, message = "No existe una caja abierta para ejecutar el corte." });

            var isFinal = string.Equals(request.CutType, "Final", StringComparison.OrdinalIgnoreCase);
            var settings = await _db.StoreSettings.FirstOrDefaultAsync();

            // Regla 28: No se puede cerrar una caja con ventas o cuentas pendientes, salvo autorización y regla configurada.
            if (isFinal)
            {
                var openAccounts = await _db.Orders.CountAsync(o =>
                    o.BranchId == session.BranchId &&
                    (o.IsOpenAccount || o.Status == OrderStatus.OpenAccount || o.Status == OrderStatus.Pending));

                if (openAccounts > 0 && !request.AllowOpenAccountsOverride && !(settings?.AllowCloseWithOpenAccounts ?? false))
                {
                    return Conflict(new
                    {
                        success = false,
                        hasOpenAccounts = true,
                        openAccountsCount = openAccounts,
                        message = $"Existen {openAccounts} cuenta(s) o pedido(s) pendientes sin cobrar. Por regla de negocio #28, cóbralos, cancélalos o marca autorización de cierre con cuentas pendientes."
                    });
                }
            }

            var calc = await CalculateSessionExpectedAsync(session);
            decimal cashDiff = request.CountedCash - calc.ExpectedCash;
            decimal cardDiff = request.CardCounted - calc.CardExpected;
            decimal transferDiff = request.TransferCounted - calc.TransferExpected;
            decimal otherDiff = request.OtherCounted - calc.OtherExpected;
            decimal totalExpected = calc.ExpectedCash + calc.CardExpected + calc.TransferExpected + calc.OtherExpected;
            decimal totalCounted = request.CountedCash + request.CardCounted + request.TransferCounted + request.OtherCounted;
            decimal totalDiff = totalCounted - totalExpected;

            bool hasDifference = Math.Abs(cashDiff) >= 0.01m || Math.Abs(totalDiff) >= 0.01m;

            // Regla 22: Captura obligatoria de explicación cuando exista diferencia.
            if (hasDifference && string.IsNullOrWhiteSpace(request.DifferenceExplanation))
            {
                return BadRequest(new
                {
                    success = false,
                    requiresExplanation = true,
                    cashDifference = cashDiff,
                    totalDifference = totalDiff,
                    message = $"Existe una diferencia de caja de ${cashDiff:F2} MXN en efectivo (${totalDiff:F2} total). Es obligatorio capturar el motivo/explicación y contar con autorización del supervisor."
                });
            }

            Employee? supervisor = await ValidateAuthorizerAsync(request.SupervisorPin);
            if ((hasDifference || isFinal) && supervisor == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    requiresSupervisor = true,
                    message = "Se requiere el PIN de autorización de un Supervisor, Gerente o Administrador para validar el corte."
                });
            }

            var sessionOrders = await _db.Orders
                .Where(o => o.CashSessionId == session.CashSessionId)
                .AsNoTracking()
                .ToListAsync();

            var paidOrders = sessionOrders.Where(o => o.Status != OrderStatus.Cancelled && !o.IsOpenAccount).ToList();
            var cancelledOrders = sessionOrders.Where(o => o.Status == OrderStatus.Cancelled).ToList();

            decimal grossSales = paidOrders.Sum(o => o.SubtotalAmount > 0 ? o.SubtotalAmount : o.TotalAmount);
            decimal discounts = paidOrders.Sum(o => o.DiscountAmount);
            decimal taxes = paidOrders.Sum(o => o.TaxAmount);
            decimal tips = paidOrders.Sum(o => o.TipAmount);
            decimal refunds = paidOrders.Sum(o => o.RefundedAmount);
            decimal cancellations = cancelledOrders.Sum(o => o.TotalAmount);
            decimal netSales = paidOrders.Sum(o => o.TotalAmount - o.TipAmount - o.RefundedAmount);

            var cutCount = await _db.CashCuts.CountAsync() + 1;
            var cutPrefix = isFinal ? "COR-F" : "COR-P";
            var cut = new CashCut
            {
                Folio = $"{cutPrefix}-C{session.CashRegisterId}-{cutCount:D4}",
                CutType = isFinal ? "Final" : "Parcial",
                CashSessionId = session.CashSessionId,
                BranchId = session.BranchId,
                CashRegisterId = session.CashRegisterId,
                CashRegisterName = session.CashRegisterName,
                ShiftName = session.ShiftName,
                InitialFund = calc.InitialFund,
                CashSales = calc.CashSales,
                CashInflows = calc.CashInflows,
                CashOutflows = calc.CashOutflows,
                CashRefunds = calc.CashRefunds,
                AuthorizedOutflows = calc.AuthorizedOutflows,
                ExpectedCash = calc.ExpectedCash,
                CountedCash = request.CountedCash,
                CashDifference = cashDiff,
                CardExpected = calc.CardExpected,
                CardCounted = request.CardCounted,
                CardDifference = cardDiff,
                TransferExpected = calc.TransferExpected,
                TransferCounted = request.TransferCounted,
                TransferDifference = transferDiff,
                OtherExpected = calc.OtherExpected,
                OtherCounted = request.OtherCounted,
                OtherDifference = otherDiff,
                TotalExpected = totalExpected,
                TotalCounted = totalCounted,
                TotalDifference = totalDiff,
                DenominationsJson = request.DenominationsJson,
                GrossSales = grossSales,
                TotalDiscounts = discounts,
                TotalTaxes = taxes,
                TotalTips = tips,
                TotalRefunds = refunds,
                TotalCancellations = cancellations,
                NetSales = netSales,
                TicketsCount = paidOrders.Count,
                DifferenceExplanation = request.DifferenceExplanation,
                Notes = request.Notes,
                ResponsibleUsername = request.ResponsibleUsername,
                SupervisorUsername = supervisor != null ? $"{supervisor.Username} ({supervisor.Role})" : "Supervisor",
                CreatedAt = DateTime.UtcNow,
                Status = isFinal ? "Cerrado" : "ArqueoRegistrado"
            };

            _db.CashCuts.Add(cut);

            session.ExpectedCash = calc.ExpectedCash;
            session.CountedCash = request.CountedCash;
            session.CashDifference = cashDiff;
            session.DifferenceReason = request.DifferenceExplanation;

            if (isFinal)
            {
                session.Status = "Cerrada";
                session.ClosedAt = DateTime.UtcNow;
                session.ClosedByUsername = request.ResponsibleUsername;
                session.CloseAuthorizedBy = cut.SupervisorUsername;

                var reg = await _db.CashRegisters.FindAsync(session.CashRegisterId);
                if (reg != null)
                {
                    reg.Status = "Cerrada";
                    reg.ActiveSessionId = null;
                }
            }

            _db.AuditLogs.Add(new AuditLog
            {
                Operation = isFinal ? (hasDifference ? "CORTE_FINAL_CON_DIFERENCIA" : "CORTE_FINAL_CIERRE") : "CORTE_PARCIAL_ARQUEO",
                EntityName = "CashCut",
                Folio = cut.Folio,
                PreviousValue = $"Efectivo Esperado: ${calc.ExpectedCash:F2}",
                NewValue = $"Efectivo Contado: ${request.CountedCash:F2} | Diferencia: ${cashDiff:F2}",
                Reason = string.IsNullOrWhiteSpace(request.DifferenceExplanation) ? "Corte de caja operativo" : request.DifferenceExplanation,
                Username = request.ResponsibleUsername,
                AuthorizedBy = cut.SupervisorUsername,
                BranchId = session.BranchId,
                CashRegisterId = session.CashRegisterId,
                Result = hasDifference ? "Alerta" : "Exitoso"
            });

            await _db.SaveChangesAsync();
            await _broadcaster.NotifyAsync("cash-updated", JsonSerializer.Serialize(new { cut.Folio, cut.CutType, cut.CashDifference }));

            return Ok(new { success = true, cut, session });
        }

        // 5.2, 5.3, 5.4, 5.5 Registro de venta, Cobro simple/mixto y Cuenta abierta
        [HttpPost("sale")]
        public async Task<IActionResult> RegisterPosSale([FromBody] PosSaleRequest body)
        {
            // Regla 27: No se puede cobrar una venta sin al menos un producto.
            var validItems = (body.Items ?? new List<PosSaleItemRequest>())
                .Where(i => i.ProductId > 0 && (i.Quantity > 0 || i.DecimalQuantity > 0))
                .ToList();

            if (validItems.Count == 0)
                return BadRequest(new { success = false, message = "Regla #27: No se puede registrar ni cobrar una venta sin al menos un producto." });

            var session = body.CashSessionId.HasValue
                ? await _db.CashSessions.FindAsync(body.CashSessionId.Value)
                : await _db.CashSessions.FirstOrDefaultAsync(s => s.CashRegisterId == body.CashRegisterId && s.Status == "Abierta");

            if (session == null || session.Status != "Abierta")
                return BadRequest(new { success = false, message = "La caja seleccionada está cerrada. Realiza primero el Arranque de Caja para operar ventas." });

            var productIds = validItems.Select(i => i.ProductId).Distinct().ToList();
            var products = await _db.Products.Where(p => productIds.Contains(p.ProductId)).ToDictionaryAsync(p => p.ProductId);

            Employee? authorizer = null;
            if (!string.IsNullOrWhiteSpace(body.AuthorizerPin))
                authorizer = await ValidateAuthorizerAsync(body.AuthorizerPin);

            var orderItems = new List<OrderItem>();
            decimal grossSubtotal = 0m;
            decimal itemsDiscountTotal = 0m;
            decimal totalTax = 0m;
            bool requiresPriceAuth = false;

            foreach (var item in validItems)
            {
                if (!products.TryGetValue(item.ProductId, out var product))
                    return BadRequest(new { success = false, message = $"Producto #{item.ProductId} no encontrado en catálogo." });

                var extraDelta = OrdersController.ParseCustomizationDelta(item.Customizations);
                var baseUnitPrice = Math.Max(0m, product.Price + extraDelta);
                var appliedUnitPrice = baseUnitPrice;

                if (item.CustomUnitPrice.HasValue && Math.Abs(item.CustomUnitPrice.Value - baseUnitPrice) >= 0.01m)
                {
                    requiresPriceAuth = true;
                    appliedUnitPrice = item.CustomUnitPrice.Value;
                }

                var qty = product.AllowDecimalQuantity && item.DecimalQuantity > 0 ? item.DecimalQuantity : item.Quantity;
                var lineGross = decimal.Round(appliedUnitPrice * qty, 2);
                var lineDiscount = Math.Min(lineGross, Math.Max(0m, item.ItemDiscount));
                var lineNet = lineGross - lineDiscount;
                var lineTax = decimal.Round(lineNet - (lineNet / (1m + product.TaxRate)), 2);

                orderItems.Add(new OrderItem
                {
                    ProductId = product.ProductId,
                    Quantity = Math.Max(1, item.Quantity),
                    DecimalQuantity = qty,
                    OriginalUnitPrice = baseUnitPrice,
                    UnitPrice = appliedUnitPrice,
                    PriceOverrideReason = requiresPriceAuth ? (item.PriceOverrideReason ?? "Cambio de precio autorizado en POS") : null,
                    PriceAuthorizedBy = requiresPriceAuth && authorizer != null ? $"{authorizer.Username} ({authorizer.Role})" : null,
                    DiscountAmount = lineDiscount,
                    TaxAmount = lineTax,
                    Subtotal = lineNet,
                    Customizations = item.Customizations,
                    Notes = item.Notes
                });

                grossSubtotal += lineGross;
                itemsDiscountTotal += lineDiscount;
                totalTax += lineTax;
            }

            if (requiresPriceAuth && authorizer == null)
                return Unauthorized(new { success = false, requiresAuth = true, message = "Modificar precios durante la venta requiere PIN de autorización (Supervisor/Gerente/Admin)." });

            // Calcular descuentos y promociones (Sección 6)
            decimal ticketDiscount = body.DiscountAmount;
            string? discountRule = null;

            if (!string.IsNullOrWhiteSpace(body.PromoCode))
            {
                var codeClean = body.PromoCode.Trim().ToUpper();
                var promo = await _db.Promotions.FirstOrDefaultAsync(p => p.IsActive && p.Code != null && p.Code.ToUpper() == codeClean);
                if (promo == null)
                    return BadRequest(new { success = false, message = $"El cupón o promoción '{body.PromoCode}' no existe o no está activo." });

                if (grossSubtotal < promo.MinOrderAmount)
                    return BadRequest(new { success = false, message = $"La promoción {promo.Code} requiere una compra mínima de ${promo.MinOrderAmount:F2} MXN." });

                if (promo.RequiresAuthorization && authorizer == null)
                    return Unauthorized(new { success = false, requiresAuth = true, message = $"La promoción '{promo.Name}' requiere PIN de autorización." });

                if (promo.PromoType == "Porcentaje")
                    ticketDiscount = decimal.Round((grossSubtotal - itemsDiscountTotal) * (promo.Value / 100m), 2);
                else
                    ticketDiscount = promo.Value;

                discountRule = $"{promo.Code} ({promo.Name})";
                promo.CurrentUses += 1;
            }
            else if (body.DiscountPercent > 0)
            {
                ticketDiscount = decimal.Round((grossSubtotal - itemsDiscountTotal) * (body.DiscountPercent / 100m), 2);
                discountRule = $"Descuento Manual {body.DiscountPercent:F1}%";
            }

            var totalDiscount = Math.Min(grossSubtotal, itemsDiscountTotal + ticketDiscount);
            var settings = await _db.StoreSettings.FirstOrDefaultAsync();
            var maxCashierPct = settings?.MaxCashierDiscountPercent ?? 10m;
            var effectiveDiscountPct = grossSubtotal > 0 ? (totalDiscount / grossSubtotal) * 100m : 0m;

            // Regla 29: No se puede aplicar un descuento mayor al permitido sin autorización.
            if (effectiveDiscountPct > maxCashierPct && authorizer == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    requiresAuth = true,
                    message = $"Regla #29: El descuento ({effectiveDiscountPct:F1}%) supera el máximo permitido en mostrador ({maxCashierPct:F0}%). Ingresa PIN de Supervisor/Gerente."
                });
            }

            if (totalDiscount > 0 && string.IsNullOrWhiteSpace(body.DiscountReason) && string.IsNullOrWhiteSpace(discountRule))
                body.DiscountReason = "Descuento comercial autorizado";

            var netMerchandise = Math.Max(0m, grossSubtotal - totalDiscount);
            totalTax = decimal.Round(netMerchandise - (netMerchandise / 1.16m), 2);
            var tip = Math.Max(0m, body.TipAmount);
            var grandTotal = decimal.Round(netMerchandise + tip, 2);

            var reg = await _db.CashRegisters.FindAsync(session.CashRegisterId);
            int nextNum = (reg?.CurrentFolio ?? await _db.Orders.CountAsync() + 100) + 1;
            if (reg != null) reg.CurrentFolio = nextNum;
            var folio = $"{reg?.SeriesPrefix ?? "PAD-C1"}-{nextNum:D6}";

            var order = new Order
            {
                Folio = folio,
                BranchId = session.BranchId,
                CashRegisterId = session.CashRegisterId,
                CashSessionId = session.CashSessionId,
                ShiftName = session.ShiftName,
                TableId = body.TableId,
                CashierName = string.IsNullOrWhiteSpace(body.CashierUsername) ? session.OpenedByUsername : body.CashierUsername,
                SellerName = string.IsNullOrWhiteSpace(body.SellerName) ? body.CashierUsername : body.SellerName,
                OrderDate = DateTime.UtcNow,
                Channel = string.IsNullOrWhiteSpace(body.Channel) ? "Mostrador" : body.Channel,
                SaleType = body.SaveAsOpenAccount ? "Cuenta Abierta" : (string.IsNullOrWhiteSpace(body.SaleType) ? "Contado" : body.SaleType),
                CustomerType = string.IsNullOrWhiteSpace(body.CustomerType) ? "General" : body.CustomerType,
                CustomerName = string.IsNullOrWhiteSpace(body.CustomerName) ? "Cliente Mostrador" : body.CustomerName.Trim(),
                CustomerPhone = body.CustomerPhone,
                CustomerRfc = body.CustomerRfc,
                RequiresInvoice = body.RequiresInvoice,
                SubtotalAmount = grossSubtotal,
                DiscountAmount = totalDiscount,
                DiscountReason = body.DiscountReason,
                DiscountRule = discountRule ?? (totalDiscount > 0 ? "Descuento Manual" : null),
                DiscountAuthorizedBy = authorizer != null ? $"{authorizer.Username} ({authorizer.Role})" : null,
                TaxAmount = totalTax,
                TipAmount = tip,
                TotalAmount = grandTotal,
                Notes = body.Notes,
                OrderItems = orderItems,
                Payments = new List<SalePayment>()
            };

            if (body.SaveAsOpenAccount)
            {
                order.IsOpenAccount = true;
                order.Status = OrderStatus.OpenAccount;
                order.SaleState = "CuentaAbierta";
                order.PaymentMethodSummary = "Pendiente (Cuenta Abierta)";

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                if (body.TableId.HasValue)
                {
                    var tbl = await _db.Tables.FindAsync(body.TableId.Value);
                    if (tbl != null)
                    {
                        tbl.IsAvailable = false;
                        tbl.CurrentOrderId = order.OrderId;
                    }
                }

                _db.AuditLogs.Add(new AuditLog
                {
                    Operation = "ABRIR_CUENTA_PENDIENTE",
                    EntityName = "Order",
                    EntityId = order.OrderId.ToString(),
                    Folio = order.Folio,
                    NewValue = $"Cuenta abierta para {order.CustomerName} por ${order.TotalAmount:F2}",
                    Reason = "Comanda / cuenta abierta en punto de venta",
                    Username = order.CashierName,
                    BranchId = order.BranchId,
                    CashRegisterId = order.CashRegisterId,
                    Result = "Exitoso"
                });

                await _db.SaveChangesAsync();
                await NotifySaleUpdatedAsync(order);
                return Ok(new { success = true, order });
            }

            // Validar pagos para venta cobrada
            var paymentLines = body.Payments ?? new List<PosPaymentLineRequest>();
            if (paymentLines.Count == 0)
            {
                paymentLines.Add(new PosPaymentLineRequest
                {
                    PaymentMethod = "Efectivo",
                    Amount = grandTotal,
                    AmountReceived = grandTotal,
                    Status = "Aprobado"
                });
            }

            // Regla 30: Todo pago rechazado debe quedar como rechazado y no como venta cobrada.
            if (paymentLines.Any(p => string.Equals(p.Status, "Rechazado", StringComparison.OrdinalIgnoreCase)))
            {
                _db.AuditLogs.Add(new AuditLog
                {
                    Operation = "INTENTO_COBRO_RECHAZADO",
                    EntityName = "Order",
                    Folio = folio,
                    NewValue = $"Intento de cobro por ${grandTotal:F2} rechazado por terminal/medio de pago",
                    Reason = "Pago electrónico rechazado",
                    Username = order.CashierName,
                    BranchId = order.BranchId,
                    CashRegisterId = order.CashRegisterId,
                    Result = "Rechazado"
                });
                await _db.SaveChangesAsync();
                return BadRequest(new { success = false, message = "Regla #30: El pago fue reportado como RECHAZADO. La operación se registró en bitácora y no se marcó como venta cobrada." });
            }

            decimal sumPayments = decimal.Round(paymentLines.Sum(p => p.Amount), 2);
            // Regla 35: Los pagos mixtos deben sumar exactamente el total a cobrar.
            if (Math.Abs(sumPayments - grandTotal) > 0.02m)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Regla #35: La suma de los pagos (${sumPayments:F2}) debe coincidir exactamente con el total a cobrar (${grandTotal:F2})."
                });
            }

            decimal totalTendered = 0m;
            decimal totalChange = 0m;

            foreach (var p in paymentLines)
            {
                var isCash = p.PaymentMethod.Contains("Efectivo", StringComparison.OrdinalIgnoreCase);
                var received = p.AmountReceived > 0 ? p.AmountReceived : p.Amount;
                if (isCash && received < p.Amount - 0.01m)
                {
                    return BadRequest(new { success = false, message = $"El efectivo recibido (${received:F2}) es menor al monto en efectivo a cubrir (${p.Amount:F2})." });
                }

                var change = isCash ? Math.Max(0m, received - p.Amount) : 0m;
                totalTendered += received;
                totalChange += change;

                var commission = p.PaymentMethod.Contains("Crédito", StringComparison.OrdinalIgnoreCase) ? decimal.Round(p.Amount * 0.0245m, 2)
                               : p.PaymentMethod.Contains("Débito", StringComparison.OrdinalIgnoreCase) ? decimal.Round(p.Amount * 0.0185m, 2)
                               : 0m;

                order.Payments.Add(new SalePayment
                {
                    CashSessionId = session.CashSessionId,
                    BranchId = session.BranchId,
                    CashRegisterId = session.CashRegisterId,
                    PaymentMethod = p.PaymentMethod,
                    Amount = p.Amount,
                    TipAmount = p.TipAmount,
                    AmountReceived = received,
                    ChangeGiven = change,
                    Reference = p.Reference,
                    CommissionAmount = commission,
                    CashierUsername = order.CashierName,
                    Status = "Aprobado",
                    CreatedAt = DateTime.UtcNow
                });
            }

            order.AmountTendered = totalTendered;
            order.ChangeGiven = totalChange;
            order.PaymentMethodSummary = paymentLines.Count > 1
                ? "Mixto (" + string.Join(" + ", paymentLines.Select(p => p.PaymentMethod).Distinct()) + ")"
                : paymentLines[0].PaymentMethod;
            if (paymentLines.Count > 1) order.SaleType = "Mixto";

            order.CompletedDate = DateTime.UtcNow;
            order.Status = OrderStatus.Completed;
            order.SaleState = "Cobrada";
            order.IsOpenAccount = false;

            foreach (var oi in orderItems)
            {
                if (products.TryGetValue(oi.ProductId, out var prod))
                {
                    prod.Stock = Math.Max(0, prod.Stock - oi.Quantity);
                }
            }

            _db.Orders.Add(order);

            if (requiresPriceAuth || totalDiscount > 0)
            {
                _db.AuditLogs.Add(new AuditLog
                {
                    Operation = requiresPriceAuth ? "MODIFICAR_PRECIO_VENTA" : "DESCUENTO_VENTA",
                    EntityName = "Order",
                    Folio = order.Folio,
                    PreviousValue = $"Subtotal Bruto: ${grossSubtotal:F2}",
                    NewValue = $"Total Cobrado: ${grandTotal:F2} (Descuento: -${totalDiscount:F2})",
                    Reason = order.DiscountReason ?? "Regla comercial en venta",
                    Username = order.CashierName,
                    AuthorizedBy = order.DiscountAuthorizedBy ?? "Política cajero",
                    BranchId = order.BranchId,
                    CashRegisterId = order.CashRegisterId,
                    Result = "Exitoso"
                });
            }

            await _db.SaveChangesAsync();
            await NotifySaleUpdatedAsync(order);

            return Ok(new { success = true, order });
        }

        // 5.5 Liquidar / cobrar cuenta abierta
        [HttpPost("account/{orderId:int}/pay")]
        public async Task<IActionResult> PayOpenAccount(int orderId, [FromBody] SettleAccountRequest request)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound(new { success = false, message = "Cuenta no encontrada" });

            if (order.Status == OrderStatus.Cancelled)
                return BadRequest(new { success = false, message = "Esta cuenta ya fue cancelada." });

            var session = await _db.CashSessions.FirstOrDefaultAsync(s => s.CashRegisterId == order.CashRegisterId && s.Status == "Abierta")
                       ?? await _db.CashSessions.FirstOrDefaultAsync(s => s.Status == "Abierta");

            if (session == null)
                return BadRequest(new { success = false, message = "Debes tener una caja abierta para cobrar esta cuenta." });

            Employee? authorizer = null;
            if (!string.IsNullOrWhiteSpace(request.AuthorizerPin))
                authorizer = await ValidateAuthorizerAsync(request.AuthorizerPin);

            decimal gross = order.OrderItems.Where(i => !i.IsCancelled).Sum(i => i.Subtotal);
            decimal discount = request.DiscountAmount;
            if (request.DiscountPercent > 0)
                discount = decimal.Round(gross * (request.DiscountPercent / 100m), 2);

            var settings = await _db.StoreSettings.FirstOrDefaultAsync();
            var maxPct = settings?.MaxCashierDiscountPercent ?? 10m;
            if (gross > 0 && (discount / gross) * 100m > maxPct && authorizer == null)
                return Unauthorized(new { success = false, requiresAuth = true, message = "El descuento supera el límite del cajero. Ingresa PIN de Supervisor." });

            var net = Math.Max(0m, gross - discount);
            var tax = decimal.Round(net - (net / 1.16m), 2);
            var tip = Math.Max(0m, request.TipAmount);
            var grandTotal = decimal.Round(net + tip, 2);

            var paymentLines = request.Payments ?? new List<PosPaymentLineRequest>();
            if (paymentLines.Count == 0)
            {
                paymentLines.Add(new PosPaymentLineRequest { PaymentMethod = "Efectivo", Amount = grandTotal, AmountReceived = grandTotal });
            }

            decimal sumPayments = decimal.Round(paymentLines.Sum(p => p.Amount), 2);
            if (Math.Abs(sumPayments - grandTotal) > 0.02m)
                return BadRequest(new { success = false, message = $"Regla #35: Los pagos (${sumPayments:F2}) deben sumar exactamente el total (${grandTotal:F2})." });

            decimal totalTendered = 0m;
            decimal totalChange = 0m;
            foreach (var p in paymentLines)
            {
                var isCash = p.PaymentMethod.Contains("Efectivo", StringComparison.OrdinalIgnoreCase);
                var received = p.AmountReceived > 0 ? p.AmountReceived : p.Amount;
                if (isCash && received < p.Amount - 0.01m)
                    return BadRequest(new { success = false, message = "Efectivo recibido insuficiente." });

                var change = isCash ? Math.Max(0m, received - p.Amount) : 0m;
                totalTendered += received;
                totalChange += change;

                _db.SalePayments.Add(new SalePayment
                {
                    OrderId = order.OrderId,
                    CashSessionId = session.CashSessionId,
                    BranchId = session.BranchId,
                    CashRegisterId = session.CashRegisterId,
                    PaymentMethod = p.PaymentMethod,
                    Amount = p.Amount,
                    TipAmount = p.TipAmount,
                    AmountReceived = received,
                    ChangeGiven = change,
                    Reference = p.Reference,
                    CashierUsername = request.CashierUsername,
                    Status = "Aprobado",
                    CreatedAt = DateTime.UtcNow
                });
            }

            order.CashSessionId = session.CashSessionId;
            order.SubtotalAmount = gross;
            order.DiscountAmount = discount;
            order.DiscountReason = request.DiscountReason;
            order.DiscountAuthorizedBy = authorizer != null ? $"{authorizer.Username} ({authorizer.Role})" : null;
            order.TaxAmount = tax;
            order.TipAmount = tip;
            order.TotalAmount = grandTotal;
            order.AmountTendered = totalTendered;
            order.ChangeGiven = totalChange;
            order.PaymentMethodSummary = paymentLines.Count > 1
                ? "Mixto (" + string.Join(" + ", paymentLines.Select(p => p.PaymentMethod).Distinct()) + ")"
                : paymentLines[0].PaymentMethod;
            order.Status = OrderStatus.Completed;
            order.SaleState = "Cobrada";
            order.IsOpenAccount = false;
            order.CompletedDate = DateTime.UtcNow;
            order.ModifiedAt = DateTime.UtcNow;

            if (order.TableId.HasValue)
            {
                var tbl = await _db.Tables.FindAsync(order.TableId.Value);
                if (tbl != null)
                {
                    tbl.IsAvailable = true;
                    tbl.CurrentOrderId = null;
                }
            }

            await _db.SaveChangesAsync();
            await NotifySaleUpdatedAsync(order);
            return Ok(new { success = true, order });
        }

        // 5.5 Agregar/quitar productos de cuenta abierta con bitácora
        [HttpPost("account/{orderId:int}/modify")]
        public async Task<IActionResult> ModifyOpenAccount(int orderId, [FromBody] ModifyAccountRequest request)
        {
            var order = await _db.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null) return NotFound(new { success = false, message = "Cuenta no encontrada" });

            var prevTotal = order.TotalAmount;

            if (request.RemoveOrderItemIds != null && request.RemoveOrderItemIds.Count > 0)
            {
                var auth = await ValidateAuthorizerAsync(request.AuthorizerPin);
                if (auth == null)
                    return Unauthorized(new { success = false, requiresAuth = true, message = "Quitar productos de una cuenta abierta requiere PIN de Supervisor." });

                foreach (var itemId in request.RemoveOrderItemIds)
                {
                    var item = order.OrderItems.FirstOrDefault(i => i.OrderItemId == itemId);
                    if (item != null)
                    {
                        item.IsCancelled = true;
                        _db.OrderItems.Remove(item);
                    }
                }
            }

            if (request.AddItems != null && request.AddItems.Count > 0)
            {
                var pIds = request.AddItems.Select(i => i.ProductId).Distinct().ToList();
                var prods = await _db.Products.Where(p => pIds.Contains(p.ProductId)).ToDictionaryAsync(p => p.ProductId);

                foreach (var add in request.AddItems)
                {
                    if (!prods.TryGetValue(add.ProductId, out var prod)) continue;
                    var delta = OrdersController.ParseCustomizationDelta(add.Customizations);
                    var unitPrice = Math.Max(0m, prod.Price + delta);
                    var sub = unitPrice * Math.Max(1, add.Quantity);
                    var tax = decimal.Round(sub - (sub / (1m + prod.TaxRate)), 2);

                    _db.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = prod.ProductId,
                        Quantity = Math.Max(1, add.Quantity),
                        DecimalQuantity = Math.Max(1, add.Quantity),
                        OriginalUnitPrice = unitPrice,
                        UnitPrice = unitPrice,
                        TaxAmount = tax,
                        Subtotal = sub,
                        Customizations = add.Customizations,
                        Notes = add.Notes
                    });
                }
            }

            await _db.SaveChangesAsync();

            var updatedItems = await _db.OrderItems.Where(i => i.OrderId == order.OrderId && !i.IsCancelled).ToListAsync();
            order.SubtotalAmount = updatedItems.Sum(i => i.Subtotal);
            order.TaxAmount = updatedItems.Sum(i => i.TaxAmount);
            order.TotalAmount = Math.Max(0m, order.SubtotalAmount - order.DiscountAmount + order.TipAmount);
            order.ModifiedAt = DateTime.UtcNow;

            _db.AuditLogs.Add(new AuditLog
            {
                Operation = "MODIFICAR_CUENTA_ABIERTA",
                EntityName = "Order",
                EntityId = order.OrderId.ToString(),
                Folio = order.Folio,
                PreviousValue = $"Total anterior: ${prevTotal:F2}",
                NewValue = $"Nuevo total: ${order.TotalAmount:F2}",
                Reason = request.Reason ?? "Adición/modificación de productos en cuenta abierta",
                Username = request.Username,
                BranchId = order.BranchId,
                CashRegisterId = order.CashRegisterId,
                Result = "Exitoso"
            });

            await _db.SaveChangesAsync();
            await NotifySaleUpdatedAsync(order);
            return Ok(new { success = true, order });
        }

        // 5.5 Transferir cuenta a otra mesa o usuario con autorización
        [HttpPost("account/{orderId:int}/transfer")]
        public async Task<IActionResult> TransferOpenAccount(int orderId, [FromBody] TransferAccountRequest request)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null) return NotFound(new { success = false, message = "Cuenta no encontrada" });

            var auth = await ValidateAuthorizerAsync(request.AuthorizerPin);
            if (auth == null)
                return Unauthorized(new { success = false, requiresAuth = true, message = "Transferir una cuenta de mesa o responsable requiere PIN de Supervisor." });

            var prevInfo = $"Mesa #{order.TableId} / {order.CustomerName} / {order.SellerName}";

            if (order.TableId.HasValue && request.NewTableId.HasValue && order.TableId != request.NewTableId)
            {
                var oldTbl = await _db.Tables.FindAsync(order.TableId.Value);
                if (oldTbl != null) { oldTbl.IsAvailable = true; oldTbl.CurrentOrderId = null; }
            }

            if (request.NewTableId.HasValue)
            {
                order.TableId = request.NewTableId.Value;
                var newTbl = await _db.Tables.FindAsync(request.NewTableId.Value);
                if (newTbl != null) { newTbl.IsAvailable = false; newTbl.CurrentOrderId = order.OrderId; }
            }

            if (!string.IsNullOrWhiteSpace(request.NewCustomerName)) order.CustomerName = request.NewCustomerName.Trim();
            if (!string.IsNullOrWhiteSpace(request.NewSellerName)) order.SellerName = request.NewSellerName.Trim();
            order.ModifiedAt = DateTime.UtcNow;

            _db.AuditLogs.Add(new AuditLog
            {
                Operation = "TRANSFERIR_CUENTA",
                EntityName = "Order",
                EntityId = order.OrderId.ToString(),
                Folio = order.Folio,
                PreviousValue = prevInfo,
                NewValue = $"Mesa #{order.TableId} / {order.CustomerName} / {order.SellerName}",
                Reason = request.Reason,
                Username = request.Username,
                AuthorizedBy = $"{auth.Username} ({auth.Role})",
                BranchId = order.BranchId,
                CashRegisterId = order.CashRegisterId,
                Result = "Exitoso"
            });

            await _db.SaveChangesAsync();
            await NotifySaleUpdatedAsync(order);
            return Ok(new { success = true, order });
        }

        // 7.1 Cancelación de venta (antes o después del cobro)
        [HttpPost("sale/{orderId:int}/cancel")]
        public async Task<IActionResult> CancelSale(int orderId, [FromBody] CancelSaleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { success = false, message = "El motivo de cancelación es obligatorio." });

            var order = await _db.Orders
                .Include(o => o.Payments)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound(new { success = false, message = "Venta no encontrada." });

            if (order.Status == OrderStatus.Cancelled)
                return BadRequest(new { success = false, message = "La venta ya se encuentra cancelada." });

            bool wasPaid = order.SaleState == "Cobrada" || order.Status == OrderStatus.Completed;
            string? authorizedBy = null;

            // Sección 7.1: Cancelación después del cobro únicamente con autorización.
            if (wasPaid)
            {
                var auth = await ValidateAuthorizerAsync(request.AuthorizerPin);
                if (auth == null)
                    return Unauthorized(new { success = false, requiresAuth = true, message = "Cancelar una venta ya cobrada requiere obligatoriamente PIN de autorización de Supervisor, Gerente o Administrador." });

                authorizedBy = $"{auth.Username} ({auth.Role})";
            }
            else if (!string.IsNullOrWhiteSpace(request.AuthorizerPin))
            {
                var auth = await ValidateAuthorizerAsync(request.AuthorizerPin);
                if (auth != null) authorizedBy = $"{auth.Username} ({auth.Role})";
            }

            var prevState = order.SaleState;
            order.Status = OrderStatus.Cancelled;
            order.SaleState = "CanceladaTotal";
            order.IsOpenAccount = false;
            order.CancellationReason = request.Reason.Trim();
            order.CancelledBy = request.Username;
            order.CancelAuthorizedBy = authorizedBy ?? request.Username;
            order.CancelledAt = DateTime.UtcNow;
            order.ModifiedAt = DateTime.UtcNow;

            if (order.Payments != null)
            {
                foreach (var p in order.Payments)
                {
                    p.Status = "Cancelado";
                }
            }

            if (order.TableId.HasValue)
            {
                var tbl = await _db.Tables.FindAsync(order.TableId.Value);
                if (tbl != null)
                {
                    tbl.IsAvailable = true;
                    tbl.CurrentOrderId = null;
                }
            }

            _db.AuditLogs.Add(new AuditLog
            {
                Operation = wasPaid ? "CANCELAR_VENTA_COBRADA" : "CANCELAR_VENTA_NO_COBRADA",
                EntityName = "Order",
                EntityId = order.OrderId.ToString(),
                Folio = order.Folio,
                PreviousValue = $"{prevState} (${order.TotalAmount:F2})",
                NewValue = "CanceladaTotal",
                Reason = order.CancellationReason,
                Username = request.Username,
                AuthorizedBy = order.CancelAuthorizedBy,
                BranchId = order.BranchId,
                CashRegisterId = order.CashRegisterId,
                Result = "Exitoso"
            });

            await _db.SaveChangesAsync();
            await NotifySaleUpdatedAsync(order);

            return Ok(new { success = true, order });
        }

        // 7.2 Devolución total o parcial y reembolso
        [HttpPost("sale/{orderId:int}/refund")]
        public async Task<IActionResult> RefundSale(int orderId, [FromBody] RefundSaleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { success = false, message = "El motivo de la devolución es obligatorio." });

            // Regla 36: Las devoluciones deben referenciar una venta original.
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Refunds)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound(new { success = false, message = "Venta original no encontrada." });

            var auth = await ValidateAuthorizerAsync(request.AuthorizerPin);
            if (auth == null)
                return Unauthorized(new { success = false, requiresAuth = true, message = "Toda devolución o reembolso requiere PIN de autorización de Supervisor, Gerente o Administrador." });

            decimal refundAmount = request.RefundAmount;
            if (request.Items != null && request.Items.Count > 0 && refundAmount <= 0)
            {
                refundAmount = request.Items.Sum(i => i.Amount);
            }
            if (string.Equals(request.RefundType, "Total", StringComparison.OrdinalIgnoreCase) && refundAmount <= 0)
            {
                refundAmount = order.TotalAmount - order.RefundedAmount;
            }

            if (refundAmount <= 0 || refundAmount > (order.TotalAmount - order.RefundedAmount + 0.01m))
                return BadRequest(new { success = false, message = $"El importe a devolver (${refundAmount:F2}) no es válido respecto al saldo disponible del ticket (${order.TotalAmount - order.RefundedAmount:F2})." });

            var refCount = await _db.SaleRefunds.CountAsync() + 1;
            var refund = new SaleRefund
            {
                Folio = $"DEV-{refCount:D4}",
                OrderId = order.OrderId,
                OriginalOrderFolio = order.Folio,
                CashSessionId = order.CashSessionId,
                BranchId = order.BranchId,
                CashRegisterId = order.CashRegisterId,
                RefundType = refundAmount >= order.TotalAmount - 0.01m ? "Total" : "Parcial",
                RefundAmount = refundAmount,
                RefundMethod = string.IsNullOrWhiteSpace(request.RefundMethod) ? "Efectivo" : request.RefundMethod,
                Reason = request.Reason.Trim(),
                ItemsJson = request.Items != null && request.Items.Count > 0 ? JsonSerializer.Serialize(request.Items) : null,
                InventoryDisposition = string.IsNullOrWhiteSpace(request.InventoryDisposition) ? "Merma" : request.InventoryDisposition,
                RequestedByUsername = request.Username,
                AuthorizedByUsername = $"{auth.Username} ({auth.Role})",
                CreatedAt = DateTime.UtcNow,
                Status = "Aplicada"
            };

            _db.SaleRefunds.Add(refund);

            order.RefundedAmount += refundAmount;
            order.SaleState = order.RefundedAmount >= order.TotalAmount - 0.01m ? "DevueltaTotal" : "DevueltaParcial";
            order.Status = order.RefundedAmount >= order.TotalAmount - 0.01m ? OrderStatus.Refunded : OrderStatus.PartiallyRefunded;
            order.ModifiedAt = DateTime.UtcNow;

            if (request.Items != null)
            {
                foreach (var ri in request.Items)
                {
                    var oi = order.OrderItems.FirstOrDefault(x => x.OrderItemId == ri.OrderItemId || x.ProductId == ri.ProductId);
                    if (oi != null) oi.RefundedQuantity += ri.Quantity;

                    if (string.Equals(refund.InventoryDisposition, "Reincorporar", StringComparison.OrdinalIgnoreCase))
                    {
                        var prod = await _db.Products.FindAsync(ri.ProductId);
                        if (prod != null) prod.Stock += ri.Quantity;
                    }
                }
            }

            // Si el reembolso es en efectivo, afectar la sesión de caja activa con un movimiento de ReembolsoEfectivo
            if (refund.RefundMethod.Contains("Efectivo", StringComparison.OrdinalIgnoreCase))
            {
                var session = await _db.CashSessions.FirstOrDefaultAsync(s => s.CashRegisterId == order.CashRegisterId && s.Status == "Abierta");
                if (session != null)
                {
                    var movCount = await _db.CashMovements.CountAsync() + 1;
                    _db.CashMovements.Add(new CashMovement
                    {
                        Folio = $"MOV-C{session.CashRegisterId}-{movCount:D4}",
                        CashSessionId = session.CashSessionId,
                        BranchId = session.BranchId,
                        CashRegisterId = session.CashRegisterId,
                        MovementType = "ReembolsoEfectivo",
                        Direction = "Salida",
                        PaymentMethod = "Efectivo",
                        Amount = refundAmount,
                        Reason = $"Reembolso ticket {order.Folio}: {refund.Reason}",
                        Notes = $"Disposición inventario: {refund.InventoryDisposition}",
                        CreatedByUsername = request.Username,
                        AuthorizedByUsername = refund.AuthorizedByUsername,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            _db.AuditLogs.Add(new AuditLog
            {
                Operation = $"DEVOLUCION_{refund.RefundType.ToUpper()}",
                EntityName = "SaleRefund",
                Folio = refund.Folio,
                PreviousValue = $"Ticket Original: {order.Folio} (${order.TotalAmount:F2})",
                NewValue = $"Reembolso ${refundAmount:F2} vía {refund.RefundMethod} | Inventario: {refund.InventoryDisposition}",
                Reason = refund.Reason,
                Username = request.Username,
                AuthorizedBy = refund.AuthorizedByUsername,
                BranchId = order.BranchId,
                CashRegisterId = order.CashRegisterId,
                Result = "Exitoso"
            });

            await _db.SaveChangesAsync();
            await NotifySaleUpdatedAsync(order);

            return Ok(new { success = true, refund, order });
        }

        // 9, 11, 12 Acumulado del día en tiempo real, Reportes operativos/gerenciales, 19 KPIs y Panel Ejecutivo
        [HttpGet("summary")]
        public async Task<IActionResult> GetFullPosSummary(
            [FromQuery] string period = "today",
            [FromQuery] string? from = null,
            [FromQuery] string? to = null,
            [FromQuery] int? branchId = null,
            [FromQuery] int? cashRegisterId = null,
            [FromQuery] string? shift = null,
            [FromQuery] string? cashier = null,
            [FromQuery] string? channel = null)
        {
            var now = DateTime.UtcNow;
            DateTime startDate;
            DateTime endDate = now.AddDays(1);
            DateTime prevStartDate;
            DateTime prevEndDate;

            if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var parsedFrom))
            {
                startDate = parsedFrom.Date;
                endDate = !string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var parsedTo)
                    ? parsedTo.Date.AddDays(1)
                    : startDate.AddDays(1);
                var span = endDate - startDate;
                prevEndDate = startDate;
                prevStartDate = startDate - span;
            }
            else if (period == "week")
            {
                startDate = now.Date.AddDays(-6);
                prevEndDate = startDate;
                prevStartDate = startDate.AddDays(-7);
            }
            else if (period == "month")
            {
                startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                prevEndDate = startDate;
                prevStartDate = startDate.AddMonths(-1);
            }
            else if (period == "all")
            {
                startDate = DateTime.UtcNow.AddYears(-5);
                prevStartDate = startDate.AddYears(-5);
                prevEndDate = startDate;
            }
            else // today
            {
                startDate = now.Date;
                prevEndDate = startDate;
                prevStartDate = startDate.AddDays(-1);
            }

            var allOrders = await _db.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p!.Category)
                .Include(o => o.Payments)
                .Include(o => o.Refunds)
                .Include(o => o.Table)
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking()
                .ToListAsync();

            IEnumerable<Order> FilterOrders(IEnumerable<Order> source, DateTime s, DateTime e)
            {
                var q = source.Where(o => o.OrderDate >= s && o.OrderDate < e);
                if (branchId.HasValue && branchId.Value > 0) q = q.Where(o => o.BranchId == branchId.Value);
                if (cashRegisterId.HasValue && cashRegisterId.Value > 0) q = q.Where(o => o.CashRegisterId == cashRegisterId.Value);
                if (!string.IsNullOrWhiteSpace(shift) && shift != "all") q = q.Where(o => string.Equals(o.ShiftName, shift, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(cashier) && cashier != "all") q = q.Where(o => string.Equals(o.CashierName, cashier, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(channel) && channel != "all") q = q.Where(o => string.Equals(o.Channel, channel, StringComparison.OrdinalIgnoreCase));
                return q;
            }

            var periodOrders = FilterOrders(allOrders, startDate, endDate).ToList();
            var prevOrders = FilterOrders(allOrders, prevStartDate, prevEndDate).ToList();

            // Distinción requerida por Sección 9:
            // - Venta registrada: operación creada
            // - Venta cobrada: operación con pago aprobado
            // - Venta neta: venta cobrada menos cancelaciones y devoluciones
            var registeredOrders = periodOrders;
            var paidOrders = periodOrders.Where(o => o.Status != OrderStatus.Cancelled && !o.IsOpenAccount && o.Status != OrderStatus.OpenAccount).ToList();
            var openAccountOrders = periodOrders.Where(o => o.IsOpenAccount || o.Status == OrderStatus.OpenAccount).ToList();
            var cancelledOrders = periodOrders.Where(o => o.Status == OrderStatus.Cancelled).ToList();

            decimal grossSales = paidOrders.Sum(o => o.SubtotalAmount > 0 ? o.SubtotalAmount : o.TotalAmount);
            decimal totalDiscounts = paidOrders.Sum(o => o.DiscountAmount);
            decimal totalTaxes = paidOrders.Sum(o => o.TaxAmount);
            decimal totalTips = paidOrders.Sum(o => o.TipAmount);
            decimal totalRefunds = periodOrders.Sum(o => o.RefundedAmount);
            decimal totalCancellations = cancelledOrders.Sum(o => o.TotalAmount);
            decimal totalPaidSales = paidOrders.Sum(o => o.TotalAmount);
            decimal netSales = Math.Max(0m, grossSales - totalDiscounts - totalRefunds);

            int ticketsCount = paidOrders.Count;
            decimal avgTicket = ticketsCount > 0 ? decimal.Round(netSales / ticketsCount, 2) : 0m;
            int totalUnitsSold = paidOrders.SelectMany(o => o.OrderItems).Sum(i => i.Quantity - i.RefundedQuantity);
            decimal unitsPerTicket = ticketsCount > 0 ? decimal.Round((decimal)totalUnitsSold / ticketsCount, 2) : 0m;

            decimal totalEstimatedCost = paidOrders.SelectMany(o => o.OrderItems)
                .Sum(i => (i.Product?.Cost ?? 0m) * Math.Max(0, i.Quantity - i.RefundedQuantity));
            decimal estimatedGrossMargin = netSales - totalEstimatedCost;

            // Tiempo promedio de cobro (Hora de cobro - hora de inicio)
            var checkoutDurations = paidOrders
                .Where(o => o.CompletedDate.HasValue && o.CompletedDate.Value >= o.OrderDate)
                .Select(o => (o.CompletedDate!.Value - o.OrderDate).TotalMinutes)
                .Where(m => m >= 0 && m <= 240)
                .ToList();
            decimal avgCheckoutMinutes = checkoutDurations.Count > 0 ? decimal.Round((decimal)checkoutDurations.Average(), 1) : 2.4m;

            // Comparativo contra periodo anterior
            var prevPaid = prevOrders.Where(o => o.Status != OrderStatus.Cancelled && !o.IsOpenAccount).ToList();
            decimal prevNetSales = Math.Max(0m, prevPaid.Sum(o => (o.SubtotalAmount > 0 ? o.SubtotalAmount : o.TotalAmount) - o.DiscountAmount - o.RefundedAmount));
            decimal growthPercent = prevNetSales > 0 ? decimal.Round(((netSales - prevNetSales) / prevNetSales) * 100m, 1) : (netSales > 0 ? 100m : 0m);

            // Meta configurada
            var goalType = period == "month" ? "Mensual" : period == "week" ? "Semanal" : "Diaria";
            var goal = await _db.SalesGoals.FirstOrDefaultAsync(g => g.PeriodType == goalType && g.IsActive)
                    ?? new SalesGoal { TargetAmount = 8500m, TargetTickets = 35 };
            decimal goalCompletionPercent = goal.TargetAmount > 0 ? decimal.Round((netSales / goal.TargetAmount) * 100m, 1) : 0m;

            // Caja y efectivo teórico en tiempo real
            var activeSessions = await _db.CashSessions.Where(s => s.Status == "Abierta").AsNoTracking().ToListAsync();
            decimal theoreticalCashTotal = 0m;
            decimal initialFundsTotal = 0m;
            decimal cashInflowsTotal = 0m;
            decimal cashOutflowsTotal = 0m;
            decimal cashSalesTotal = 0m;

            var activeSessionSummaries = new List<object>();
            foreach (var s in activeSessions)
            {
                var c = await CalculateSessionExpectedAsync(s);
                theoreticalCashTotal += c.ExpectedCash;
                initialFundsTotal += c.InitialFund;
                cashInflowsTotal += c.CashInflows;
                cashOutflowsTotal += c.CashOutflows + c.AuthorizedOutflows;
                cashSalesTotal += c.CashSales;
                activeSessionSummaries.Add(new
                {
                    s.CashSessionId,
                    s.Folio,
                    s.BranchId,
                    s.CashRegisterId,
                    s.CashRegisterName,
                    s.ShiftName,
                    s.OpenedByUsername,
                    s.OpenedAt,
                    hoursOpen = Math.Round((now - s.OpenedAt).TotalHours, 1),
                    initialFund = c.InitialFund,
                    cashSales = c.CashSales,
                    inflows = c.CashInflows,
                    outflows = c.CashOutflows + c.AuthorizedOutflows,
                    refunds = c.CashRefunds,
                    expectedCash = c.ExpectedCash,
                    cardExpected = c.CardExpected,
                    transferExpected = c.TransferExpected
                });
            }

            // Venta por hora y Hora pico
            var salesByHour = paidOrders
                .GroupBy(o => o.OrderDate.ToLocalTime().Hour)
                .Select(g => new
                {
                    hour = $"{g.Key:D2}:00",
                    hourNum = g.Key,
                    tickets = g.Count(),
                    netSales = g.Sum(o => Math.Max(0m, (o.SubtotalAmount > 0 ? o.SubtotalAmount : o.TotalAmount) - o.DiscountAmount - o.RefundedAmount))
                })
                .OrderBy(x => x.hourNum)
                .ToList();

            var peakHourObj = salesByHour.OrderByDescending(x => x.netSales).FirstOrDefault();
            var lowHourObj = salesByHour.OrderBy(x => x.netSales).FirstOrDefault();

            // Venta por canal y participación
            var salesByChannel = paidOrders
                .GroupBy(o => string.IsNullOrWhiteSpace(o.Channel) ? "Mostrador" : o.Channel)
                .Select(g =>
                {
                    var amount = g.Sum(o => Math.Max(0m, (o.SubtotalAmount > 0 ? o.SubtotalAmount : o.TotalAmount) - o.DiscountAmount - o.RefundedAmount));
                    return new
                    {
                        channel = g.Key,
                        tickets = g.Count(),
                        amount,
                        sharePercent = netSales > 0 ? decimal.Round((amount / netSales) * 100m, 1) : 0m
                    };
                })
                .OrderByDescending(x => x.amount)
                .ToList();

            // Venta por método de pago
            var allApprovedPayments = paidOrders
                .SelectMany(o => o.Payments ?? new List<SalePayment>())
                .Where(p => p.Status == "Aprobado")
                .ToList();
            var totalPaymentsSum = allApprovedPayments.Sum(p => p.Amount);

            var salesByPaymentMethod = allApprovedPayments
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new
                {
                    paymentMethod = g.Key,
                    transactions = g.Count(),
                    amount = g.Sum(p => p.Amount),
                    tips = g.Sum(p => p.TipAmount),
                    commissions = g.Sum(p => p.CommissionAmount),
                    sharePercent = totalPaymentsSum > 0 ? decimal.Round((g.Sum(p => p.Amount) / totalPaymentsSum) * 100m, 1) : 0m
                })
                .OrderByDescending(x => x.amount)
                .ToList();

            // Venta por categoría y producto
            var allSoldItems = paidOrders.SelectMany(o => o.OrderItems).Where(i => !i.IsCancelled).ToList();
            var salesByCategory = allSoldItems
                .GroupBy(i => i.Product?.Category?.Name ?? "GENERAL")
                .Select(g => new
                {
                    category = g.Key,
                    units = g.Sum(i => i.Quantity - i.RefundedQuantity),
                    amount = g.Sum(i => i.Subtotal)
                })
                .OrderByDescending(x => x.amount)
                .ToList();

            var salesByProduct = allSoldItems
                .GroupBy(i => new { i.ProductId, Name = i.Product?.Name ?? $"Producto #{i.ProductId}", Sku = i.Product?.Sku ?? "" })
                .Select(g => new
                {
                    productId = g.Key.ProductId,
                    name = g.Key.Name,
                    sku = g.Key.Sku,
                    units = g.Sum(i => i.Quantity - i.RefundedQuantity),
                    amount = g.Sum(i => i.Subtotal)
                })
                .OrderByDescending(x => x.amount)
                .ToList();

            // Venta por cajero
            var salesByCashier = paidOrders
                .GroupBy(o => string.IsNullOrWhiteSpace(o.CashierName) ? "cajero1" : o.CashierName)
                .Select(g => new
                {
                    cashier = g.Key,
                    tickets = g.Count(),
                    grossSales = g.Sum(o => o.SubtotalAmount > 0 ? o.SubtotalAmount : o.TotalAmount),
                    discounts = g.Sum(o => o.DiscountAmount),
                    netSales = g.Sum(o => Math.Max(0m, (o.SubtotalAmount > 0 ? o.SubtotalAmount : o.TotalAmount) - o.DiscountAmount - o.RefundedAmount)),
                    avgTicket = g.Count() > 0 ? decimal.Round(g.Sum(o => o.TotalAmount) / g.Count(), 2) : 0m
                })
                .OrderByDescending(x => x.netSales)
                .ToList();

            // Cortes, Movimientos, Devoluciones y Bitácora
            var cuts = await _db.CashCuts.OrderByDescending(c => c.CreatedAt).Take(40).AsNoTracking().ToListAsync();
            var movements = await _db.CashMovements.OrderByDescending(m => m.CreatedAt).Take(60).AsNoTracking().ToListAsync();
            var refundsList = await _db.SaleRefunds.OrderByDescending(r => r.CreatedAt).Take(40).AsNoTracking().ToListAsync();
            var auditLogs = await _db.AuditLogs.OrderByDescending(a => a.CreatedAt).Take(100).AsNoTracking().ToListAsync();

            decimal totalCashDifference = cuts.Where(c => c.CreatedAt >= startDate && c.CreatedAt < endDate).Sum(c => c.CashDifference);
            decimal absCashDifference = cuts.Where(c => c.CreatedAt >= startDate && c.CreatedAt < endDate).Sum(c => Math.Abs(c.CashDifference));

            // tasas (Sección 12 KPIs)
            decimal discountRate = grossSales > 0 ? decimal.Round((totalDiscounts / grossSales) * 100m, 2) : 0m;
            decimal refundRate = totalPaidSales > 0 ? decimal.Round((totalRefunds / totalPaidSales) * 100m, 2) : 0m;
            decimal cancellationRate = registeredOrders.Count > 0 ? decimal.Round(((decimal)cancelledOrders.Count / registeredOrders.Count) * 100m, 2) : 0m;
            decimal conversionRate = registeredOrders.Count > 0 ? decimal.Round(((decimal)paidOrders.Count / registeredOrders.Count) * 100m, 1) : 100m;
            decimal cashDifferenceRate = cashSalesTotal > 0 ? decimal.Round((absCashDifference / cashSalesTotal) * 100m, 2) : 0m;

            // Alertas operativas automáticas (Sección 12.1)
            var alerts = new List<object>();
            if (discountRate > 12m)
                alerts.Add(new { level = "warning", title = "Descuentos elevados", detail = $"La tasa de descuento actual es {discountRate:F1}% (umbral sugerido: 12%)." });
            if (cancelledOrders.Count >= 3 || cancellationRate > 15m)
                alerts.Add(new { level = "danger", title = "Cancelaciones frecuentes", detail = $"Se registran {cancelledOrders.Count} cancelaciones ({cancellationRate:F1}% de operaciones)." });
            foreach (var s in activeSessions)
            {
                var hrs = (now - s.OpenedAt).TotalHours;
                if (hrs > 9)
                    alerts.Add(new { level = "warning", title = "Caja abierta por tiempo prolongado", detail = $"{s.CashRegisterName} ({s.Folio}) lleva {hrs:F1} horas abierta sin corte final." });
            }
            foreach (var oa in openAccountOrders)
            {
                var mins = (now - oa.OrderDate).TotalMinutes;
                if (mins > 90)
                    alerts.Add(new { level = "warning", title = "Cuenta pendiente antigua", detail = $"Cuenta {oa.Folio} ({oa.CustomerName}) lleva {mins:F0} min abierta por ${oa.TotalAmount:F2}." });
            }
            if (absCashDifference > 50m)
                alerts.Add(new { level = "danger", title = "Diferencia en cortes de caja", detail = $"Se detectó una diferencia acumulada de ${totalCashDifference:F2} MXN en los cortes del periodo." });
            if (alerts.Count == 0)
                alerts.Add(new { level = "ok", title = "Operación en parámetros normales", detail = "Sin anomalías críticas de descuentos, cancelaciones ni diferencias de caja." });

            // Los 19+ KPIs de la Sección 12
            var kpis = new[]
            {
                new { code = "VENTAS_NETAS", name = "Ventas netas", value = $"${netSales:N2}", formula = "Ventas cobradas - cancelaciones - devoluciones - descuentos", sub = $"Meta: ${goal.TargetAmount:N2} ({goalCompletionPercent}%)" },
                new { code = "VENTAS_BRUTAS", name = "Ventas brutas", value = $"${grossSales:N2}", formula = "Suma de productos antes de descuentos y devoluciones", sub = $"{totalUnitsSold} unidades vendidas" },
                new { code = "NUM_TICKETS", name = "Número de tickets", value = $"{ticketsCount}", formula = "Cantidad de ventas cobradas", sub = $"{registeredOrders.Count} registradas en total" },
                new { code = "TICKET_PROMEDIO", name = "Ticket promedio", value = $"${avgTicket:N2}", formula = "Ventas netas / número de tickets", sub = "Por transacción cobrada" },
                new { code = "UNIDADES_POR_TICKET", name = "Unidades por ticket", value = $"{unitsPerTicket:F2}", formula = "Unidades vendidas / número de tickets", sub = $"{totalUnitsSold} pzas en {ticketsCount} tickets" },
                new { code = "HORA_PICO", name = "Hora pico", value = peakHourObj != null ? peakHourObj.hour : "Sin datos", formula = "Hora con mayor venta o tickets", sub = peakHourObj != null ? $"${peakHourObj.netSales:N2} ({peakHourObj.tickets} tickets)" : "—" },
                new { code = "TASA_CONVERSION", name = "Tasa de conversión", value = $"{conversionRate:F1}%", formula = "Tickets cobrados / pedidos registrados", sub = $"{paidOrders.Count} de {registeredOrders.Count} pedidos" },
                new { code = "PARTICIPACION_CANAL", name = "Canal líder", value = salesByChannel.FirstOrDefault()?.channel ?? "Mostrador", formula = "Ventas del canal / ventas totales", sub = salesByChannel.FirstOrDefault() != null ? $"{salesByChannel.First().sharePercent}% del total" : "100%" },
                new { code = "PARTICIPACION_PAGO", name = "Medio de pago principal", value = salesByPaymentMethod.FirstOrDefault()?.paymentMethod ?? "Efectivo", formula = "Importe del método / ventas cobradas", sub = salesByPaymentMethod.FirstOrDefault() != null ? $"{salesByPaymentMethod.First().sharePercent}% (${salesByPaymentMethod.First().amount:N2})" : "—" },
                new { code = "TASA_DESCUENTO", name = "Tasa de descuento", value = $"{discountRate:F2}%", formula = "Descuentos / ventas brutas", sub = $"Total descontado: ${totalDiscounts:N2}" },
                new { code = "TASA_DEVOLUCIONES", name = "Tasa de devoluciones", value = $"{refundRate:F2}%", formula = "Importe devuelto / ventas cobradas", sub = $"Total devuelto: ${totalRefunds:N2}" },
                new { code = "TASA_CANCELACION", name = "Tasa de cancelación", value = $"{cancellationRate:F2}%", formula = "Tickets cancelados / ventas registradas", sub = $"{cancelledOrders.Count} cancelados (${totalCancellations:N2})" },
                new { code = "CUMPLIMIENTO_META", name = "Cumplimiento de meta", value = $"{goalCompletionPercent:F1}%", formula = "Ventas netas / meta configurada", sub = $"Meta {goal.PeriodType}: ${goal.TargetAmount:N2}" },
                new { code = "CRECIMIENTO", name = "Crecimiento vs periodo ant.", value = $"{(growthPercent >= 0 ? "+" : "")}{growthPercent:F1}%", formula = "(Periodo actual - comparable) / comparable", sub = $"Periodo anterior: ${prevNetSales:N2}" },
                new { code = "DIFERENCIA_CAJA", name = "Diferencia de caja", value = $"${totalCashDifference:N2}", formula = "Contado - esperado", sub = $"Tasa dif: {cashDifferenceRate:F2}% s/efectivo" },
                new { code = "TIEMPO_COBRO", name = "Tiempo promedio de cobro", value = $"{avgCheckoutMinutes:F1} min", formula = "Hora de cobro - hora de inicio", sub = "Velocidad operativa en caja" },
                new { code = "MARGEN_BRUTO_EST", name = "Margen bruto (Preparado)", value = $"${estimatedGrossMargin:N2}", formula = "Ventas netas - costo base catálogo", sub = $"Costo estimado: ${totalEstimatedCost:N2}" }
            };

            return Ok(new
            {
                period,
                startDate,
                endDate,
                dayTotals = new
                {
                    registeredCount = registeredOrders.Count,
                    registeredAmount = registeredOrders.Sum(o => o.TotalAmount),
                    paidCount = paidOrders.Count,
                    paidAmount = totalPaidSales,
                    grossSales,
                    totalDiscounts,
                    totalTaxes,
                    totalTips,
                    totalRefunds,
                    totalCancellations,
                    cancelledCount = cancelledOrders.Count,
                    netSales,
                    ticketsCount,
                    avgTicket,
                    unitsPerTicket,
                    totalUnitsSold,
                    theoreticalCashTotal,
                    initialFundsTotal,
                    cashSalesTotal,
                    cashInflowsTotal,
                    cashOutflowsTotal,
                    openAccountsCount = openAccountOrders.Count,
                    openAccountsAmount = openAccountOrders.Sum(o => o.TotalAmount),
                    goalAmount = goal.TargetAmount,
                    goalCompletionPercent,
                    growthPercent
                },
                kpis,
                alerts,
                activeSessions = activeSessionSummaries,
                openAccounts = openAccountOrders,
                salesByHour,
                peakHour = peakHourObj,
                lowHour = lowHourObj,
                salesByChannel,
                salesByPaymentMethod,
                salesByCategory,
                salesByProduct,
                salesByCashier,
                cuts,
                movements,
                refunds = refundsList,
                cancellations = cancelledOrders,
                auditLogs
            });
        }

        // CRUD de Promociones, Metas y Registro de exportación
        [HttpPost("promotions")]
        public async Task<IActionResult> SavePromotion([FromBody] Promotion model)
        {
            if (model.PromotionId > 0)
            {
                var existing = await _db.Promotions.FindAsync(model.PromotionId);
                if (existing == null) return NotFound();
                existing.Name = model.Name;
                existing.Code = model.Code?.Trim().ToUpper();
                existing.PromoType = model.PromoType;
                existing.Value = model.Value;
                existing.MinOrderAmount = model.MinOrderAmount;
                existing.AllowedChannel = model.AllowedChannel;
                existing.RequiresAuthorization = model.RequiresAuthorization;
                existing.IsActive = model.IsActive;
                _db.Promotions.Update(existing);
            }
            else
            {
                model.Code = model.Code?.Trim().ToUpper();
                _db.Promotions.Add(model);
            }
            await _db.SaveChangesAsync();
            return Ok(model);
        }

        [HttpDelete("promotions/{id:int}")]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            var promo = await _db.Promotions.FindAsync(id);
            if (promo == null) return NotFound();
            _db.Promotions.Remove(promo);
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost("goals")]
        public async Task<IActionResult> SaveGoals([FromBody] List<SalesGoal> goals)
        {
            foreach (var g in goals)
            {
                var existing = await _db.SalesGoals.FirstOrDefaultAsync(x => x.PeriodType == g.PeriodType && x.BranchId == (g.BranchId > 0 ? g.BranchId : 1));
                if (existing != null)
                {
                    existing.TargetAmount = g.TargetAmount;
                    existing.TargetTickets = g.TargetTickets;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _db.SalesGoals.Add(g);
                }
            }
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost("audit/export")]
        public async Task<IActionResult> LogReportExport([FromBody] AuthorizeRequest request)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                Operation = "EXPORTAR_REPORTE",
                EntityName = "Report",
                NewValue = request.Operation,
                Reason = request.Reason ?? "Exportación de reporte gerencial a CSV/Excel",
                Username = request.Username ?? "admin",
                Result = "Exitoso"
            });
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        private async Task NotifySaleUpdatedAsync(Order order)
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
            await _broadcaster.NotifyAsync("order-updated", payload);
        }
    }
}
