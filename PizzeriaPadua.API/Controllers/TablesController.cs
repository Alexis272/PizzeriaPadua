using PizzeriaPadua.API.Data;
using PizzeriaPadua.Core.Data;
using PizzeriaPadua.Core.ViewModels.Table;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PizzeriaPadua.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TablesController : ControllerBase
    {
        private readonly PizzeriaPaduaContext _context;
        private readonly ILogger<TablesController> _logger;

        public TablesController(PizzeriaPaduaContext context, ILogger<TablesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTables()
        {
            var tables = await _context.Tables.AsNoTracking().ToListAsync();
            return Ok(tables);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTable(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null) return NotFound();
            return Ok(table);
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableTables()
        {
            var tables = await _context.Tables.Where(t => t.IsAvailable).AsNoTracking().ToListAsync();
            return Ok(tables);
        }

        [HttpPut("add")]
        public async Task<IActionResult> AddTable([FromBody] AddTableView model)
        {
            var table = new Table
            {
                TableNumber = model.TableNumber,
                Area = string.IsNullOrWhiteSpace(model.Area) ? "Salón Principal" : model.Area,
                Capacity = model.Capacity > 0 ? model.Capacity : 4,
                IsAvailable = model.IsAvailable
            };
            _context.Tables.Add(table);
            await _context.SaveChangesAsync();
            return Ok(table);
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateTable([FromBody] UpdateTableView model)
        {
            var table = await _context.Tables.FindAsync(model.TableId);
            if (table == null) return NotFound();

            table.TableNumber = model.TableNumber;
            if (!string.IsNullOrWhiteSpace(model.Area)) table.Area = model.Area;
            table.Capacity = model.Capacity;
            table.IsAvailable = model.IsAvailable;

            _context.Tables.Update(table);
            await _context.SaveChangesAsync();
            return Ok(table);
        }

        [HttpDelete("delete/{id:int}")]
        public async Task<IActionResult> DeleteTable(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null) return NotFound();
            _context.Tables.Remove(table);
            await _context.SaveChangesAsync();
            return Ok(table);
        }
    }
}
