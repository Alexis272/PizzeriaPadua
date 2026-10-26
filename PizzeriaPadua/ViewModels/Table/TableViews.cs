using System.ComponentModel.DataAnnotations;

namespace PizzeriaPadua.Core.ViewModels.Table
{
    public class AddTableView
    {
        [Required]
        public string TableNumber { get; set; } = "";
        public string Area { get; set; } = "Salón Principal";
        public int Capacity { get; set; } = 4;
        public bool IsAvailable { get; set; } = true;
    }

    public class UpdateTableView
    {
        public int TableId { get; set; }
        [Required]
        public string TableNumber { get; set; } = "";
        public string Area { get; set; } = "Salón Principal";
        public int Capacity { get; set; } = 4;
        public bool IsAvailable { get; set; } = true;
    }
}
