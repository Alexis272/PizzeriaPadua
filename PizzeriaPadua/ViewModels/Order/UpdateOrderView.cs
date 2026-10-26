namespace PizzeriaPadua.Core.ViewModels.Order
{
    public class UpdateOrderView
    {
        public int OrderId { get; set; }
        public int? TableId { get; set; }
        public int? EmployeeId { get; set; }
        public int Status { get; set; }
    }
}
