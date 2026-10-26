using System;
using System.ComponentModel.DataAnnotations;

namespace PizzeriaPadua.Core.Data
{
    public class Branch
    {
        public int BranchId { get; set; }
        [StringLength(150)]
        public string CompanyName { get; set; } = "Pizzería Padua S.A. de C.V.";
        [StringLength(20)]
        public string Rfc { get; set; } = "PPA260101MX1";
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";
        [StringLength(30)]
        public string Code { get; set; } = "SUC-01";
        [StringLength(250)]
        public string Address { get; set; } = "";
        [StringLength(30)]
        public string Phone { get; set; } = "";
        [StringLength(10)]
        public string Currency { get; set; } = "MXN";
        public decimal DefaultTaxRate { get; set; } = 0.16m;
        [StringLength(20)]
        public string SeriesPrefix { get; set; } = "PAD";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CashRegister
    {
        public int CashRegisterId { get; set; }
        public int BranchId { get; set; } = 1;
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";
        [StringLength(30)]
        public string Code { get; set; } = "CAJA-01";
        [StringLength(100)]
        public string? PrinterName { get; set; } = "Impresora Térmica 80mm";
        [StringLength(20)]
        public string SeriesPrefix { get; set; } = "C1";
        public int CurrentFolio { get; set; } = 100;
        public bool IsActive { get; set; } = true;
        [StringLength(30)]
        public string Status { get; set; } = "Cerrada";
        public int? ActiveSessionId { get; set; }
    }

    public class ShiftConfig
    {
        public int ShiftConfigId { get; set; }
        public int BranchId { get; set; } = 1;
        [Required]
        [StringLength(60)]
        public string Name { get; set; } = "";
        [StringLength(10)]
        public string StartTime { get; set; } = "10:00";
        [StringLength(10)]
        public string EndTime { get; set; } = "18:00";
        public decimal SuggestedInitialFund { get; set; } = 1500m;
        public bool IsActive { get; set; } = true;
    }

    public class CashSession
    {
        public int CashSessionId { get; set; }
        [StringLength(50)]
        public string Folio { get; set; } = "";
        public int BranchId { get; set; } = 1;
        public int CashRegisterId { get; set; } = 1;
        [StringLength(100)]
        public string CashRegisterName { get; set; } = "Caja 1";
        [StringLength(60)]
        public string ShiftName { get; set; } = "Matutino";
        public int? OpenedByEmployeeId { get; set; }
        [StringLength(80)]
        public string OpenedByUsername { get; set; } = "";
        [StringLength(80)]
        public string? AuthorizedByUsername { get; set; }
        public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }
        public decimal InitialFund { get; set; }
        [StringLength(2000)]
        public string? DenominationsJson { get; set; }
        [StringLength(500)]
        public string? OpeningNotes { get; set; }
        [StringLength(30)]
        public string Status { get; set; } = "Abierta";
        public decimal ExpectedCash { get; set; }
        public decimal CountedCash { get; set; }
        public decimal CashDifference { get; set; }
        [StringLength(500)]
        public string? DifferenceReason { get; set; }
        [StringLength(80)]
        public string? ClosedByUsername { get; set; }
        [StringLength(80)]
        public string? CloseAuthorizedBy { get; set; }
    }

    public class CashMovement
    {
        public int CashMovementId { get; set; }
        [StringLength(50)]
        public string Folio { get; set; } = "";
        public int CashSessionId { get; set; }
        public int BranchId { get; set; } = 1;
        public int CashRegisterId { get; set; } = 1;
        [StringLength(50)]
        public string MovementType { get; set; } = "EntradaExtraordinaria";
        [StringLength(30)]
        public string Direction { get; set; } = "Entrada";
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Efectivo";
        public decimal Amount { get; set; }
        [StringLength(200)]
        public string Reason { get; set; } = "";
        [StringLength(500)]
        public string? Notes { get; set; }
        [StringLength(80)]
        public string CreatedByUsername { get; set; } = "";
        [StringLength(80)]
        public string? AuthorizedByUsername { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CashCut
    {
        public int CashCutId { get; set; }
        [StringLength(50)]
        public string Folio { get; set; } = "";
        [StringLength(30)]
        public string CutType { get; set; } = "Parcial";
        public int CashSessionId { get; set; }
        public int BranchId { get; set; } = 1;
        public int CashRegisterId { get; set; } = 1;
        [StringLength(100)]
        public string CashRegisterName { get; set; } = "";
        [StringLength(60)]
        public string ShiftName { get; set; } = "";
        public decimal InitialFund { get; set; }
        public decimal CashSales { get; set; }
        public decimal CashInflows { get; set; }
        public decimal CashOutflows { get; set; }
        public decimal CashRefunds { get; set; }
        public decimal AuthorizedOutflows { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal CountedCash { get; set; }
        public decimal CashDifference { get; set; }
        public decimal CardExpected { get; set; }
        public decimal CardCounted { get; set; }
        public decimal CardDifference { get; set; }
        public decimal TransferExpected { get; set; }
        public decimal TransferCounted { get; set; }
        public decimal TransferDifference { get; set; }
        public decimal OtherExpected { get; set; }
        public decimal OtherCounted { get; set; }
        public decimal OtherDifference { get; set; }
        public decimal TotalExpected { get; set; }
        public decimal TotalCounted { get; set; }
        public decimal TotalDifference { get; set; }
        [StringLength(2000)]
        public string? DenominationsJson { get; set; }
        public decimal GrossSales { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal TotalTaxes { get; set; }
        public decimal TotalTips { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal TotalCancellations { get; set; }
        public decimal NetSales { get; set; }
        public int TicketsCount { get; set; }
        [StringLength(500)]
        public string? DifferenceExplanation { get; set; }
        [StringLength(500)]
        public string? Notes { get; set; }
        [StringLength(80)]
        public string ResponsibleUsername { get; set; } = "";
        [StringLength(80)]
        public string? SupervisorUsername { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(30)]
        public string Status { get; set; } = "Cerrado";
    }

    public class SalePayment
    {
        public int SalePaymentId { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        public int? CashSessionId { get; set; }
        public int BranchId { get; set; } = 1;
        public int CashRegisterId { get; set; } = 1;
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Efectivo";
        public decimal Amount { get; set; }
        public decimal TipAmount { get; set; }
        public decimal AmountReceived { get; set; }
        public decimal ChangeGiven { get; set; }
        [StringLength(120)]
        public string? Reference { get; set; }
        public decimal CommissionAmount { get; set; }
        [StringLength(80)]
        public string CashierUsername { get; set; } = "";
        [StringLength(30)]
        public string Status { get; set; } = "Aprobado";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SaleRefund
    {
        public int SaleRefundId { get; set; }
        [StringLength(50)]
        public string Folio { get; set; } = "";
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        [StringLength(50)]
        public string OriginalOrderFolio { get; set; } = "";
        public int? CashSessionId { get; set; }
        public int BranchId { get; set; } = 1;
        public int CashRegisterId { get; set; } = 1;
        [StringLength(30)]
        public string RefundType { get; set; } = "Parcial";
        public decimal RefundAmount { get; set; }
        [StringLength(50)]
        public string RefundMethod { get; set; } = "Efectivo";
        [StringLength(250)]
        public string Reason { get; set; } = "";
        [StringLength(2000)]
        public string? ItemsJson { get; set; }
        [StringLength(40)]
        public string InventoryDisposition { get; set; } = "Merma";
        [StringLength(80)]
        public string RequestedByUsername { get; set; } = "";
        [StringLength(80)]
        public string AuthorizedByUsername { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [StringLength(30)]
        public string Status { get; set; } = "Aplicada";
    }

    public class Promotion
    {
        public int PromotionId { get; set; }
        [Required]
        [StringLength(120)]
        public string Name { get; set; } = "";
        [StringLength(40)]
        public string? Code { get; set; }
        [StringLength(40)]
        public string PromoType { get; set; } = "Porcentaje";
        public decimal Value { get; set; }
        public decimal MinOrderAmount { get; set; } = 0m;
        public int? CategoryId { get; set; }
        public int? ProductId { get; set; }
        [StringLength(60)]
        public string AllowedChannel { get; set; } = "Todos";
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        [StringLength(30)]
        public string DaysOfWeek { get; set; } = "1,2,3,4,5,6,0";
        [StringLength(10)]
        public string StartHour { get; set; } = "00:00";
        [StringLength(10)]
        public string EndHour { get; set; } = "23:59";
        public int MaxUsesPerDay { get; set; } = 0;
        public int CurrentUses { get; set; } = 0;
        public bool AllowStacking { get; set; } = false;
        public bool RequiresAuthorization { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }

    public class SalesGoal
    {
        public int SalesGoalId { get; set; }
        public int BranchId { get; set; } = 1;
        [StringLength(30)]
        public string PeriodType { get; set; } = "Diaria";
        public decimal TargetAmount { get; set; } = 15000m;
        public int TargetTickets { get; set; } = 60;
        public decimal MaxCashDifferenceRate { get; set; } = 0.5m;
        public bool IsActive { get; set; } = true;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CatalogReason
    {
        public int CatalogReasonId { get; set; }
        [StringLength(40)]
        public string ReasonType { get; set; } = "Cancelacion";
        [StringLength(200)]
        public string Description { get; set; } = "";
        public bool RequiresAuthorization { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }

    public class PaymentMethodConfig
    {
        public int PaymentMethodConfigId { get; set; }
        [StringLength(60)]
        public string Name { get; set; } = "";
        [StringLength(30)]
        public string Code { get; set; } = "";
        public bool RequiresReference { get; set; } = false;
        public bool AllowsTip { get; set; } = true;
        public decimal CommissionPercentage { get; set; } = 0m;
        public bool AffectsCashDrawer { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }

    public class SalesChannelConfig
    {
        public int SalesChannelConfigId { get; set; }
        [StringLength(60)]
        public string Name { get; set; } = "";
        [StringLength(30)]
        public string Code { get; set; } = "";
        public decimal PriceMultiplier { get; set; } = 1.0m;
        public bool IsActive { get; set; } = true;
    }

    public class AuditLog
    {
        public int AuditLogId { get; set; }
        [StringLength(80)]
        public string Operation { get; set; } = "";
        [StringLength(60)]
        public string EntityName { get; set; } = "";
        [StringLength(60)]
        public string? EntityId { get; set; }
        [StringLength(60)]
        public string? Folio { get; set; }
        [StringLength(1000)]
        public string? PreviousValue { get; set; }
        [StringLength(1000)]
        public string? NewValue { get; set; }
        [StringLength(300)]
        public string? Reason { get; set; }
        [StringLength(80)]
        public string Username { get; set; } = "";
        [StringLength(80)]
        public string? AuthorizedBy { get; set; }
        public int BranchId { get; set; } = 1;
        public int CashRegisterId { get; set; } = 1;
        [StringLength(120)]
        public string DeviceInfo { get; set; } = "Terminal POS Web";
        [StringLength(40)]
        public string Result { get; set; } = "Exitoso";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
