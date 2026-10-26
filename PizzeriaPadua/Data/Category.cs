using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PizzeriaPadua.Core.Data
{
    public class Category
    {
        public int CategoryId { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";
        [StringLength(500)]
        public string Description { get; set; } = "";
        [StringLength(100)]
        public string? Subcategory { get; set; }
        public decimal DefaultTaxRate { get; set; } = 0.16m;
        public bool IsActive { get; set; } = true;
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
