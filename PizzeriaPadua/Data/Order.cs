using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PizzeriaPadua.Core.Data
{
    public class Order
    {
        public int OrderId { get; set; }
        [StringLength(50)]
        public string Folio { get; set; } = "";
        public int BranchId { get; set; } = 1;
        public int CashRegisterId { get; set; } = 1;
        public int? CashSessionId { get; set; }
        [StringLength(50)]
        public string ShiftName { get; set; } = "Matutino";
        public int? UserId { get; set; }
        public User? User { get; set; }
        public int? TableId { get; set; }
        public Table? Table { get; set; }
        public int? EmployeeId { get; set; }
        public Employee? Employee { get; set; }
        [StringLength(80)]
        public string CashierName { get; set; } = "Mostrador";
        [StringLength(80)]
        public string? SellerName { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedDate { get; set; }
        public DateTime? ModifiedAt { get; set; }
        [StringLength(50)]
        public string Channel { get; set; } = "Mostrador";
        [StringLength(50)]
        public string SaleType { get; set; } = "Contado";
        [StringLength(50)]
        public string CustomerType { get; set; } = "General";
        public string? CustomerName { get; set; }
        [StringLength(30)]
        public string? CustomerPhone { get; set; }
        [StringLength(20)]
        public string? CustomerRfc { get; set; }
        public bool RequiresInvoice { get; set; } = false;
        public decimal SubtotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        [StringLength(200)]
        public string? DiscountReason { get; set; }
        [StringLength(100)]
        public string? DiscountRule { get; set; }
        [StringLength(80)]
        public string? DiscountAuthorizedBy { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TipAmount { get; set; }
        public decimal RoundingAdjustment { get; set; }
        public decimal TotalAmount { get; set; }
        [StringLength(100)]
        public string PaymentMethodSummary { get; set; } = "Efectivo";
        public decimal AmountTendered { get; set; }
        public decimal ChangeGiven { get; set; }
        public Enums.OrderStatus Status { get; set; }
        [StringLength(40)]
        public string SaleState { get; set; } = "Registrada";
        public bool IsOpenAccount { get; set; } = false;
        [StringLength(500)]
        public string? Notes { get; set; }
        [StringLength(250)]
        public string? CancellationReason { get; set; }
        [StringLength(80)]
        public string? CancelledBy { get; set; }
        [StringLength(80)]
        public string? CancelAuthorizedBy { get; set; }
        public DateTime? CancelledAt { get; set; }
        public decimal RefundedAmount { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<SalePayment> Payments { get; set; } = new List<SalePayment>();
        public ICollection<SaleRefund> Refunds { get; set; } = new List<SaleRefund>();
    }
}
