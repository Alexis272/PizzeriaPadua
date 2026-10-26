using PizzeriaPadua.API.Data;
using PizzeriaPadua.API.Services;
using PizzeriaPadua.Core.Data;
using PizzeriaPadua.Core.ViewModels.Product;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PizzeriaPadua.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly PizzeriaPaduaContext _context;
        private readonly ILogger<ProductsController> _logger;
        private readonly EventBroadcaster _broadcaster;

        public ProductsController(PizzeriaPaduaContext context, ILogger<ProductsController> logger, EventBroadcaster broadcaster)
        {
            _context = context;
            _logger = logger;
            _broadcaster = broadcaster;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting products: {Error}", ex);
                return BadRequest("Internal Error");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                    return NotFound();

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting product: {Error}", ex);
                return BadRequest("Internal Error");
            }
        }

        [HttpGet("by-category/{categoryId:int}")]
        public async Task<IActionResult> GetProductsByCategory(int categoryId)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.CategoryId == categoryId)
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting products by category: {Error}", ex);
                return BadRequest("Internal Error");
            }
        }

        [HttpPut("add")]
        public async Task<IActionResult> AddProduct([FromBody] AddProductView model)
        {
            try
            {
                var nameUpper = model.Name.Trim().ToUpper();
                var product = new Product
                {
                    Name = nameUpper,
                    ShortName = string.IsNullOrWhiteSpace(model.ShortName) ? (nameUpper.Length > 18 ? nameUpper[..18] : nameUpper) : model.ShortName.Trim().ToUpper(),
                    Sku = string.IsNullOrWhiteSpace(model.Sku) ? $"SKU-{Random.Shared.Next(1000, 9999)}" : model.Sku.Trim().ToUpper(),
                    Barcode = string.IsNullOrWhiteSpace(model.Barcode) ? $"750{Random.Shared.Next(100000000, 999999999)}" : model.Barcode.Trim(),
                    Description = model.Description ?? "",
                    Price = model.Price,
                    Cost = model.Cost,
                    TaxRate = model.TaxRate >= 0 ? model.TaxRate : 0.16m,
                    UnitOfSale = string.IsNullOrWhiteSpace(model.UnitOfSale) ? "PZA" : model.UnitOfSale.ToUpper(),
                    AllowDecimalQuantity = model.AllowDecimalQuantity,
                    IsCombo = model.IsCombo,
                    CategoryId = model.CategoryId,
                    Stock = model.Stock > 0 ? model.Stock : 50,
                    IsAvailable = model.IsAvailable,
                    ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? "/img/pizza-padua.svg" : model.ImageUrl,
                    IsFeatured = model.IsFeatured,
                    Badge = model.Badge,
                    CustomizationOptions = model.CustomizationOptions,
                    AvailableChannels = string.IsNullOrWhiteSpace(model.AvailableChannels) ? "Todos" : model.AvailableChannels
                };

                _context.Products.Add(product);
                _context.AuditLogs.Add(new AuditLog
                {
                    Operation = "ALTA_PRODUCTO",
                    EntityName = "Product",
                    Folio = product.Sku,
                    NewValue = $"{product.Name} (${product.Price:F2})",
                    Reason = "Alta en catálogo de productos",
                    Username = "admin",
                    Result = "Exitoso"
                });
                await _context.SaveChangesAsync();
                _ = _broadcaster.NotifyAsync("product-changed", "{}");

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error adding product: {Error}", ex);
                return BadRequest("Internal Error");
            }
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateProduct([FromBody] UpdateProductView model)
        {
            try
            {
                var product = await _context.Products.FindAsync(model.ProductId);
                if (product == null)
                    return NotFound();

                var prev = $"{product.Name} (${product.Price:F2})";
                var nameUpper = model.Name.Trim().ToUpper();
                product.Name = nameUpper;
                if (!string.IsNullOrWhiteSpace(model.ShortName)) product.ShortName = model.ShortName.Trim().ToUpper();
                if (!string.IsNullOrWhiteSpace(model.Sku)) product.Sku = model.Sku.Trim().ToUpper();
                if (!string.IsNullOrWhiteSpace(model.Barcode)) product.Barcode = model.Barcode.Trim();
                product.Description = model.Description ?? "";
                product.Price = model.Price;
                if (model.Cost >= 0) product.Cost = model.Cost;
                if (model.TaxRate >= 0) product.TaxRate = model.TaxRate;
                if (!string.IsNullOrWhiteSpace(model.UnitOfSale)) product.UnitOfSale = model.UnitOfSale.ToUpper();
                product.AllowDecimalQuantity = model.AllowDecimalQuantity;
                product.IsCombo = model.IsCombo;
                product.CategoryId = model.CategoryId;
                product.Stock = model.Stock;
                product.IsAvailable = model.IsAvailable;
                if (!string.IsNullOrWhiteSpace(model.ImageUrl)) product.ImageUrl = model.ImageUrl;
                product.IsFeatured = model.IsFeatured;
                product.Badge = model.Badge;
                product.CustomizationOptions = model.CustomizationOptions;
                if (!string.IsNullOrWhiteSpace(model.AvailableChannels)) product.AvailableChannels = model.AvailableChannels;

                _context.Products.Update(product);
                _context.AuditLogs.Add(new AuditLog
                {
                    Operation = "EDITAR_PRODUCTO",
                    EntityName = "Product",
                    EntityId = product.ProductId.ToString(),
                    Folio = product.Sku,
                    PreviousValue = prev,
                    NewValue = $"{product.Name} (${product.Price:F2})",
                    Reason = "Actualización en catálogo",
                    Username = "admin",
                    Result = "Exitoso"
                });
                await _context.SaveChangesAsync();
                _ = _broadcaster.NotifyAsync("product-changed", "{}");

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error updating product: {Error}", ex);
                return BadRequest("Internal Error");
            }
        }

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage([FromForm] int productId, [FromForm] IFormFile file)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return NotFound(new { success = false, message = "Product not found" });

                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "No file provided" });

                var ext = Path.GetExtension(file.FileName);
                if (string.IsNullOrEmpty(ext))
                    ext = ".png";

                var fileName = $"{Guid.NewGuid()}{ext}";
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var filePath = Path.Combine(uploadsDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                product.ImageUrl = $"/img/{fileName}";
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
                _ = _broadcaster.NotifyAsync("product-changed", "{}");

                return Ok(new { success = true, imageUrl = product.ImageUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error uploading image: {Error}", ex);
                return BadRequest(new { success = false, message = "Internal Error" });
            }
        }

        [HttpDelete("delete/{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound();

                var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
                if (hasOrders)
                {
                    // No borrar físicamente si tiene historial de ventas; desactivar y auditar
                    product.IsAvailable = false;
                    _context.Products.Update(product);
                }
                else
                {
                    _context.Products.Remove(product);
                }

                _context.AuditLogs.Add(new AuditLog
                {
                    Operation = "BAJA_PRODUCTO",
                    EntityName = "Product",
                    EntityId = product.ProductId.ToString(),
                    Folio = product.Sku,
                    PreviousValue = $"{product.Name} (Activo)",
                    NewValue = hasOrders ? "Inactivo (con historial de ventas)" : "Eliminado",
                    Reason = "Baja solicitada en catálogo",
                    Username = "admin",
                    Result = "Exitoso"
                });

                await _context.SaveChangesAsync();
                _ = _broadcaster.NotifyAsync("product-changed", "{}");

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error deleting product: {Error}", ex);
                return BadRequest("Internal Error");
            }
        }
    }
}
