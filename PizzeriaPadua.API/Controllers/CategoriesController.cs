using PizzeriaPadua.API.Data;
using PizzeriaPadua.API.Services;
using PizzeriaPadua.Core.Data;
using PizzeriaPadua.Core.ViewModels.Category;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PizzeriaPadua.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly PizzeriaPaduaContext _context;
        private readonly ILogger<CategoriesController> _logger;
        private readonly EventBroadcaster _broadcaster;

        public CategoriesController(PizzeriaPaduaContext context, ILogger<CategoriesController> logger, EventBroadcaster broadcaster)
        {
            _context = context;
            _logger = logger;
            _broadcaster = broadcaster;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.Products)
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting categories: {ex}");
                return BadRequest("Internal Error");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.CategoryId == id);

                if (category == null)
                    return NotFound();

                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting category: {ex}");
                return BadRequest("Internal Error");
            }
        }

        [HttpPut("add")]
        public async Task<IActionResult> AddCategory([FromBody] AddCategoryView model)
        {
            try
            {
                var category = new Category
                {
                    Name = model.Name.ToUpper(),
                    Description = model.Description ?? "",
                    Subcategory = model.Subcategory,
                    DefaultTaxRate = model.DefaultTaxRate > 0 ? model.DefaultTaxRate : 0.16m
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                _ = _broadcaster.NotifyAsync("product-changed", "{}");

                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding category: {ex}");
                return BadRequest("Internal Error");
            }
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateCategory([FromBody] UpdateCategoryView model)
        {
            try
            {
                var category = await _context.Categories.FindAsync(model.CategoryId);
                if (category == null)
                    return NotFound();

                category.Name = model.Name.ToUpper();
                category.Description = model.Description ?? "";
                category.Subcategory = model.Subcategory;
                if (model.DefaultTaxRate >= 0) category.DefaultTaxRate = model.DefaultTaxRate;

                _context.Categories.Update(category);
                await _context.SaveChangesAsync();
                _ = _broadcaster.NotifyAsync("product-changed", "{}");

                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating category: {ex}");
                return BadRequest("Internal Error");
            }
        }

        [HttpDelete("delete/{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                    return NotFound();

                await _context.Products
                    .Where(p => p.CategoryId == id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.CategoryId, (int?)null));

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                _ = _broadcaster.NotifyAsync("product-changed", "{}");

                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting category: {ex}");
                return BadRequest("Internal Error");
            }
        }
    }
}
