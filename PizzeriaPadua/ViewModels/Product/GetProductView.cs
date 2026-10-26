namespace PizzeriaPadua.Core.ViewModels.Product
{
    public class GetProductView
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public int? CategoryId { get; set; }
        public int Stock { get; set; }
        public bool IsAvailable { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsFeatured { get; set; }
        public string? Badge { get; set; }
        public string? CustomizationOptions { get; set; }
    }
}
