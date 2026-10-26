using System.ComponentModel.DataAnnotations;

namespace PizzeriaPadua.Core.ViewModels.Product
{
    public class UpdateProductView
    {
        public int ProductId { get; set; }
        [Required]
        public string Name { get; set; } = "";
        public string? ShortName { get; set; }
        public string? Sku { get; set; }
        public string? Barcode { get; set; }
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public decimal Cost { get; set; } = 0m;
        public decimal TaxRate { get; set; } = 0.16m;
        public string UnitOfSale { get; set; } = "PZA";
        public bool AllowDecimalQuantity { get; set; } = false;
        public bool IsCombo { get; set; } = false;
        public int? CategoryId { get; set; }
        public int Stock { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string? ImageUrl { get; set; }
        public bool IsFeatured { get; set; } = false;
        public string? Badge { get; set; }
        public string? CustomizationOptions { get; set; }
        public string? AvailableChannels { get; set; } = "Todos";
    }
}
