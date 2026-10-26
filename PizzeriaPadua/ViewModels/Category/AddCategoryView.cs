using System.ComponentModel.DataAnnotations;

namespace PizzeriaPadua.Core.ViewModels.Category
{
    public class AddCategoryView
    {
        [Required]
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string? Subcategory { get; set; }
        public decimal DefaultTaxRate { get; set; } = 0.16m;
    }
}
