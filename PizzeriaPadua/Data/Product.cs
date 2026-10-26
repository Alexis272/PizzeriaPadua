using System.ComponentModel.DataAnnotations;

namespace PizzeriaPadua.Core.Data
{
    public class Product
    {
        public int ProductId { get; set; }
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = "";
        [StringLength(80)]
        public string? ShortName { get; set; }
        [StringLength(60)]
        public string? Sku { get; set; }
        [StringLength(80)]
        public string? Barcode { get; set; }
        [StringLength(1000)]
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public decimal Cost { get; set; } = 0m;
        public decimal TaxRate { get; set; } = 0.16m;
        public bool TaxIncluded { get; set; } = true;
        [StringLength(30)]
        public string UnitOfSale { get; set; } = "PZA";
        public bool AllowDecimalQuantity { get; set; } = false;
        public bool IsCombo { get; set; } = false;
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        public int Stock { get; set; }
        public bool IsAvailable { get; set; } = true;
        [StringLength(500)]
        public string? ImageUrl { get; set; }
        public bool IsFeatured { get; set; } = false;
        [StringLength(50)]
        public string? Badge { get; set; }
        [StringLength(2000)]
        public string? CustomizationOptions { get; set; }
        [StringLength(500)]
        public string? AvailableChannels { get; set; } = "Todos";
        [StringLength(1000)]
        public string? AlternatePricesJson { get; set; }
    }
}
