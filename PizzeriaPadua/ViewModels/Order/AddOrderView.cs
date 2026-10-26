namespace PizzeriaPadua.Core.ViewModels.Order
{
    public class AddOrderView
    {
        public int? UserId { get; set; }
        public int? TableId { get; set; }
        public int? EmployeeId { get; set; }
        public string? CustomerName { get; set; }
        public string? Channel { get; set; }
        public string? Notes { get; set; }
    }
}
