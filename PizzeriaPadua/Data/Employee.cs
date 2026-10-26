using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PizzeriaPadua.Core.Data
{
    public class Employee
    {
        public int EmployeeId { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";
        [StringLength(100)]
        public string LastName { get; set; } = "";
        [StringLength(20)]
        public string Phone { get; set; } = "";
        [StringLength(100)]
        public string Position { get; set; } = "";
        [StringLength(60)]
        public string Username { get; set; } = "";
        [StringLength(255)]
        public string Password { get; set; } = "";
        [StringLength(20)]
        public string PinCode { get; set; } = "";
        [StringLength(50)]
        public string Role { get; set; } = "Cajero";
        public int BranchId { get; set; } = 1;
        public int? AllowedCashRegisterId { get; set; }
        public bool CanModifyPrice { get; set; } = false;
        public bool CanAuthorizeDiscount { get; set; } = false;
        public bool CanCancelPaidSale { get; set; } = false;
        public bool CanProcessRefund { get; set; } = false;
        public bool CanManageCatalog { get; set; } = false;
        public bool CanOpenCloseCash { get; set; } = true;
        public bool CanRegisterCashMovement { get; set; } = false;
        public bool CanAdjustCashDifference { get; set; } = false;
        public bool CanViewFinancialReports { get; set; } = false;
        public bool CanExportReports { get; set; } = false;
        public decimal MaxManualDiscountPercent { get; set; } = 10m;
        public bool IsActive { get; set; } = true;
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
