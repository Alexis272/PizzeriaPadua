using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PizzeriaPadua.Core.Data
{
    public class Table
    {
        public int TableId { get; set; }
        [Required]
        [StringLength(50)]
        public string TableNumber { get; set; } = "";
        [StringLength(50)]
        public string Area { get; set; } = "Salón Principal";
        public int Capacity { get; set; } = 4;
        public bool IsAvailable { get; set; } = true;
        public int? CurrentOrderId { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
