namespace PizzeriaPadua.Core.ViewModels.Order
{
    public class AddOrderItemView
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string? Customizations { get; set; }
        public decimal? OverrideUnitPrice { get; set; }
    }
}
