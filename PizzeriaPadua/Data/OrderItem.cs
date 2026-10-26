using System.ComponentModel.DataAnnotations;

namespace PizzeriaPadua.Core.Data
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int Quantity { get; set; }
        public decimal DecimalQuantity { get; set; } = 1m;
        public decimal OriginalUnitPrice { get; set; }
        public decimal UnitPrice { get; set; }
        [StringLength(200)]
        public string? PriceOverrideReason { get; set; }
        [StringLength(80)]
        public string? PriceAuthorizedBy { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Subtotal { get; set; }
        [StringLength(2000)]
        public string? Customizations { get; set; }
        [StringLength(300)]
        public string? Notes { get; set; }
        public bool IsCancelled { get; set; } = false;
        public int RefundedQuantity { get; set; } = 0;
    }
}
