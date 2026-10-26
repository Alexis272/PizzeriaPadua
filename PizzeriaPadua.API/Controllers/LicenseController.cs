using PizzeriaPadua.API.Data;
using PizzeriaPadua.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PizzeriaPadua.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class LicenseController : ControllerBase
    {
        private readonly LicensingService _licensing;
        private readonly ILogger<LicenseController> _logger;
        private readonly AdminAuthService _adminAuth;
        private readonly PizzeriaPaduaContext _context;

        public LicenseController(LicensingService licensing, ILogger<LicenseController> logger, AdminAuthService adminAuth, PizzeriaPaduaContext context)
        {
            _licensing = licensing;
            _logger = logger;
            _adminAuth = adminAuth;
            _context = context;
        }

        [HttpPost("auth")]
        public async Task<IActionResult> Auth([FromBody] LicenseAuthRequest request)
        {
            var u = (request.Username ?? "").Trim();
            var p = request.Password ?? "";

            var emp = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.IsActive &&
                    (e.Username.ToLower() == u.ToLower() && (e.Password == p || e.PinCode == p)));

            if (emp != null)
            {
                return Ok(new
                {
                    success = true,
                    message = "Authenticated",
                    staff = new
                    {
                        emp.EmployeeId,
                        emp.Name,
                        emp.LastName,
                        emp.Username,
                        emp.Role,
                        emp.Position,
                        emp.BranchId,
                        emp.AllowedCashRegisterId,
                        emp.CanModifyPrice,
                        emp.CanAuthorizeDiscount,
                        emp.CanCancelPaidSale,
                        emp.CanProcessRefund,
                        emp.CanManageCatalog,
                        emp.CanOpenCloseCash,
                        emp.CanRegisterCashMovement,
                        emp.CanAdjustCashDifference,
                        emp.CanViewFinancialReports,
                        emp.CanExportReports,
                        emp.MaxManualDiscountPercent
                    }
                });
            }

            if (_adminAuth.ValidateCredentials(u, p))
            {
                return Ok(new
                {
                    success = true,
                    message = "Authenticated",
                    staff = new
                    {
                        EmployeeId = 1,
                        Name = "Administrador",
                        LastName = "General",
                        Username = u,
                        Role = "Administrador",
                        Position = "Administrador General",
                        BranchId = 1,
                        AllowedCashRegisterId = (int?)null,
                        CanModifyPrice = true,
                        CanAuthorizeDiscount = true,
                        CanCancelPaidSale = true,
                        CanProcessRefund = true,
                        CanManageCatalog = true,
                        CanOpenCloseCash = true,
                        CanRegisterCashMovement = true,
                        CanAdjustCashDifference = true,
                        CanViewFinancialReports = true,
                        CanExportReports = true,
                        MaxManualDiscountPercent = 100m
                    }
                });
            }

            return Unauthorized(new { success = false, message = "Usuario o contraseña incorrectos" });
        }

        [HttpPost("upload")]
        public IActionResult Upload([FromBody] LicenseUploadRequest request)
        {
            if (!ValidateAuthHeader())
                return Unauthorized(new { success = false, message = "Authentication required" });

            var success = _licensing.UploadLicense(request.LicenseContent);
            if (success)
                return Ok(new { success = true, message = "License uploaded", expires = _licensing.ExpiryDateFormatted });
            return BadRequest(new { success = false, message = "Invalid license file" });
        }

        [HttpGet("status")]
        public IActionResult Status()
        {
            return Ok(new
            {
                licensed = _licensing.IsLicensed,
                expired = _licensing.IsExpired,
                expires = _licensing.ExpiryDateFormatted,
                expiryDate = _licensing.ExpiryDate?.ToString("O")
            });
        }

        [HttpPost("revoke")]
        public IActionResult Revoke()
        {
            if (!ValidateAuthHeader())
                return Unauthorized(new { success = false, message = "Authentication required" });

            _licensing.RevokeLicense();
            return Ok(new { success = true, message = "License revoked" });
        }

        private bool ValidateAuthHeader()
        {
            var auth = Request.Headers["X-License-Auth"].FirstOrDefault();
            return _adminAuth.ValidateToken(auth);
        }
    }

    public class LicenseAuthRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class LicenseUploadRequest
    {
        public string LicenseContent { get; set; } = "";
    }
}
