using PizzeriaPadua.API.Data;
using PizzeriaPadua.Core.Data;
using PizzeriaPadua.Core.ViewModels.Employee;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PizzeriaPadua.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly PizzeriaPaduaContext _context;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(PizzeriaPaduaContext context, ILogger<EmployeesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEmployees()
        {
            var employees = await _context.Employees.AsNoTracking().ToListAsync();
            return Ok(employees);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();
            return Ok(employee);
        }

        [HttpPut("add")]
        public async Task<IActionResult> AddEmployee([FromBody] AddEmployeeView model)
        {
            var role = string.IsNullOrWhiteSpace(model.Role) ? "Cajero" : model.Role;
            var isSupervisorOrAbove = role is "Administrador" or "Gerente" or "Supervisor";

            var employee = new Employee
            {
                Name = model.Name,
                LastName = model.LastName ?? "",
                Phone = model.Phone ?? "",
                Position = string.IsNullOrWhiteSpace(model.Position) ? role : model.Position,
                Username = string.IsNullOrWhiteSpace(model.Username) ? model.Name.ToLower().Replace(" ", "") : model.Username.Trim().ToLower(),
                Password = string.IsNullOrWhiteSpace(model.Password) ? "padua2026" : model.Password,
                PinCode = string.IsNullOrWhiteSpace(model.PinCode) ? "4444" : model.PinCode,
                Role = role,
                BranchId = model.BranchId > 0 ? model.BranchId : 1,
                CanModifyPrice = isSupervisorOrAbove,
                CanAuthorizeDiscount = isSupervisorOrAbove,
                CanCancelPaidSale = isSupervisorOrAbove,
                CanProcessRefund = isSupervisorOrAbove,
                CanManageCatalog = role is "Administrador" or "Gerente",
                CanOpenCloseCash = role != "Auditor",
                CanRegisterCashMovement = isSupervisorOrAbove,
                CanAdjustCashDifference = isSupervisorOrAbove,
                CanViewFinancialReports = isSupervisorOrAbove || role == "Auditor",
                CanExportReports = role is "Administrador" or "Gerente" or "Auditor",
                MaxManualDiscountPercent = role == "Administrador" ? 100m : role == "Gerente" ? 50m : role == "Supervisor" ? 25m : 10m,
                IsActive = true
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            return Ok(employee);
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateEmployee([FromBody] UpdateEmployeeView model)
        {
            var employee = await _context.Employees.FindAsync(model.EmployeeId);
            if (employee == null) return NotFound();

            var role = string.IsNullOrWhiteSpace(model.Role) ? employee.Role : model.Role;
            var isSupervisorOrAbove = role is "Administrador" or "Gerente" or "Supervisor";

            employee.Name = model.Name;
            employee.LastName = model.LastName ?? "";
            employee.Phone = model.Phone ?? "";
            employee.Position = string.IsNullOrWhiteSpace(model.Position) ? role : model.Position;
            if (!string.IsNullOrWhiteSpace(model.Username)) employee.Username = model.Username.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(model.Password)) employee.Password = model.Password;
            if (!string.IsNullOrWhiteSpace(model.PinCode)) employee.PinCode = model.PinCode;
            employee.Role = role;
            employee.BranchId = model.BranchId > 0 ? model.BranchId : employee.BranchId;
            employee.IsActive = model.IsActive;
            employee.CanModifyPrice = isSupervisorOrAbove;
            employee.CanAuthorizeDiscount = isSupervisorOrAbove;
            employee.CanCancelPaidSale = isSupervisorOrAbove;
            employee.CanProcessRefund = isSupervisorOrAbove;
            employee.CanManageCatalog = role is "Administrador" or "Gerente";
            employee.CanOpenCloseCash = role != "Auditor";
            employee.CanRegisterCashMovement = isSupervisorOrAbove;
            employee.CanAdjustCashDifference = isSupervisorOrAbove;
            employee.CanViewFinancialReports = isSupervisorOrAbove || role == "Auditor";
            employee.CanExportReports = role is "Administrador" or "Gerente" or "Auditor";

            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
            return Ok(employee);
        }

        [HttpDelete("delete/{id:int}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();
            employee.IsActive = false;
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
            return Ok(employee);
        }
    }
}
