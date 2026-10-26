using System.ComponentModel.DataAnnotations;

namespace PizzeriaPadua.Core.ViewModels.Employee
{
    public class AddEmployeeView
    {
        [Required]
        public string Name { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Position { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string PinCode { get; set; } = "";
        public string Role { get; set; } = "Cajero";
        public int BranchId { get; set; } = 1;
    }

    public class UpdateEmployeeView
    {
        public int EmployeeId { get; set; }
        [Required]
        public string Name { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Position { get; set; } = "";
        public string Username { get; set; } = "";
        public string? Password { get; set; }
        public string PinCode { get; set; } = "";
        public string Role { get; set; } = "Cajero";
        public int BranchId { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }
}
