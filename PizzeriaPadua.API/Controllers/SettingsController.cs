using PizzeriaPadua.Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaPadua.API.Data;
using PizzeriaPadua.API.Services;

namespace PizzeriaPadua.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly PizzeriaPaduaContext _db;
        private readonly EventBroadcaster _broadcaster;
        private readonly AdminAuthService _adminAuth;

        public SettingsController(PizzeriaPaduaContext db, EventBroadcaster broadcaster, AdminAuthService adminAuth)
        {
            _db = db;
            _broadcaster = broadcaster;
            _adminAuth = adminAuth;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var settings = await _db.StoreSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new StoreSettings { OpeningTime = "11:00", ClosingTime = "23:00", Address = "" };
                _db.StoreSettings.Add(settings);
                await _db.SaveChangesAsync();
            }
            return Ok(settings);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] StoreSettings updated)
        {
            if (!ValidateAuthHeader())
                return Unauthorized(new { success = false, message = "Authentication required" });

            var settings = await _db.StoreSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new StoreSettings();
                _db.StoreSettings.Add(settings);
            }
            settings.OpeningTime = updated.OpeningTime ?? "11:00";
            settings.ClosingTime = updated.ClosingTime ?? "23:00";
            settings.Address = updated.Address ?? "";
            if (!string.IsNullOrWhiteSpace(updated.CompanyName)) settings.CompanyName = updated.CompanyName;
            if (!string.IsNullOrWhiteSpace(updated.Rfc)) settings.Rfc = updated.Rfc;
            if (!string.IsNullOrWhiteSpace(updated.Currency)) settings.Currency = updated.Currency;
            if (updated.DefaultTaxRate >= 0) settings.DefaultTaxRate = updated.DefaultTaxRate;
            if (!string.IsNullOrWhiteSpace(updated.RoundingRule)) settings.RoundingRule = updated.RoundingRule;
            settings.TipsEnabled = updated.TipsEnabled;
            settings.AllowCloseWithOpenAccounts = updated.AllowCloseWithOpenAccounts;
            if (updated.MaxCashierDiscountPercent >= 0) settings.MaxCashierDiscountPercent = updated.MaxCashierDiscountPercent;
            if (!string.IsNullOrWhiteSpace(updated.ReturnPolicyText)) settings.ReturnPolicyText = updated.ReturnPolicyText;

            await _db.SaveChangesAsync();
            await _broadcaster.NotifyAsync("settings-changed", "{}");

            return Ok(new { success = true, message = "Settings saved" });
        }

        [HttpPost("upload-promo-media")]
        public async Task<IActionResult> UploadPromoMedia([FromForm] IFormFile file)
        {
            if (!ValidateAuthHeader())
                return Unauthorized(new { success = false, message = "Authentication required" });

            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "No file provided" });

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg", ".mp4", ".webm", ".mov" };
                if (!allowed.Contains(ext))
                    return BadRequest(new { success = false, message = "Formato no permitido" });

                var mediaDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "promo");
                if (!Directory.Exists(mediaDir))
                    Directory.CreateDirectory(mediaDir);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(mediaDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var mediaUrl = $"/promo/{fileName}";
                return Ok(new
                {
                    success = true,
                    mediaUrl,
                    mediaType = ext == ".mp4" || ext == ".webm" || ext == ".mov" ? "video" : "image"
                });
            }
            catch
            {
                return BadRequest(new { success = false, message = "Internal Error" });
            }
        }

        private bool ValidateAuthHeader()
        {
            var auth = Request.Headers["X-License-Auth"].FirstOrDefault();
            return _adminAuth.ValidateToken(auth);
        }
    }
}
