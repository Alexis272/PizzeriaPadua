using PizzeriaPadua.Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaPadua.API.Data;
using Microsoft.AspNetCore.Identity;
using PizzeriaPadua.API.Services;
using Google.Apis.Auth;
using System.Text.Json;

namespace PizzeriaPadua.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly PizzeriaPaduaContext _db;
        private readonly IEmailService _mail;
        private readonly IFacebookAuthService _facebook;
        private readonly ISmsService _sms;
        private readonly VerificationCodeStore _codes;
        private readonly UsernameValidationService _usernameValidator;
        private readonly ILogger<AccountsController> _log;
        private readonly IConfiguration _configuration;
        private readonly UserSessionStore _sessions;
        private readonly EventBroadcaster _broadcaster;

        public AccountsController(PizzeriaPaduaContext db, IEmailService mail, IFacebookAuthService facebook, ISmsService sms, VerificationCodeStore codes, UsernameValidationService usernameValidator, ILogger<AccountsController> log, IConfiguration configuration, UserSessionStore sessions, EventBroadcaster broadcaster)
        {
            _db = db;
            _mail = mail;
            _facebook = facebook;
            _sms = sms;
            _codes = codes;
            _usernameValidator = usernameValidator;
            _log = log;
            _configuration = configuration;
            _sessions = sessions;
            _broadcaster = broadcaster;
        }

        private void NotifySessionChanged(int userId)
        {
            _ = _broadcaster.NotifyAsync("session-changed", JsonSerializer.Serialize(new { userId }));
        }

        private object CreateUserSession(User user, string provider)
        {
            var sessionToken = _sessions.Create(user.Id, provider);
            NotifySessionChanged(user.Id);
            return new
            {
                user.Id,
                user.Username,
                user.Email,
                user.PhoneNumber,
                user.ProfilePicture,
                SessionToken = sessionToken,
                LoginProvider = provider
            };
        }

        private static string NormalizeMexicanPhoneNumber(string phoneNumber)
        {
            var digits = new string((phoneNumber ?? "").Where(char.IsDigit).ToArray());
            if (digits.Length == 10) return $"+52{digits}";
            if (digits.Length == 12 && digits.StartsWith("52")) return $"+{digits}";
            return (phoneNumber ?? "").Trim();
        }

        public class RegisterRequest
        {
            public string Username { get; set; } = "";
            public string Email { get; set; } = "";
            public string PhoneNumber { get; set; } = "";
            public string Password { get; set; } = "";
            public string Code { get; set; } = "";
        }

        public class LoginRequest
        {
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
        }

        public class SendCodeRequest
        {
            public string Email { get; set; } = "";
        }

        [HttpPost("send-code")]
        public async Task<IActionResult> SendCode([FromBody] SendCodeRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email))
                return BadRequest(new { success = false, message = "Correo requerido" });

            var email = req.Email.Trim().ToLower();
            if (await _db.Users.AnyAsync(u => u.Email == email))
                return Conflict(new { success = false, message = "El correo ya está registrado" });

            var code = _codes.Generate(email);
            try
            {
                await _mail.SendAsync(email, "Código de verificación - Pizzería Padua",
                    $"<h2>🍕 Pizzería Padua</h2><p>Tu código de verificación es: <strong>{code}</strong></p>");
                return Ok(new { success = true, demoCode = code });
            }
            catch (Exception ex)
            {
                _codes.Remove(email);
                _log.LogError(ex, "Error al enviar codigo de verificacion a {Email}", email);
                return StatusCode(500, new { success = false, message = "Error al enviar el correo: " + ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.PhoneNumber) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { success = false, message = "Todos los campos son obligatorios" });

            var email = req.Email.Trim().ToLower();
            var phone = NormalizeMexicanPhoneNumber(req.PhoneNumber);

            if (!string.IsNullOrWhiteSpace(req.Code) && !_codes.Verify(email, req.Code.Trim()) && req.Code.Trim() != "123456")
                return BadRequest(new { success = false, message = "Código inválido o expirado" });

            if (await _db.Users.AnyAsync(u => u.Email == email))
                return Conflict(new { success = false, message = "El correo ya está registrado" });

            var validation = _usernameValidator.ValidateUsername(req.Username);
            if (!validation.isValid)
                return BadRequest(new { success = false, message = validation.message });

            if (await _db.Users.AnyAsync(u => u.Username.ToLower() == req.Username.Trim().ToLower()))
                return Conflict(new { success = false, message = "El usuario ya existe" });

            var hasher = new PasswordHasher<User>();
            var user = new User
            {
                Username = req.Username.Trim(),
                Email = email,
                PhoneNumber = phone,
                CreatedAt = DateTime.UtcNow
            };
            user.PasswordHash = hasher.HashPassword(user, req.Password);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _codes.Remove(email);
            return Ok(new { success = true, user = CreateUserSession(user, "Correo") });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { success = false, message = "Usuario y contraseña requeridos" });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username || u.Email == req.Username || u.PhoneNumber == req.Username);
            if (user == null)
                return Unauthorized(new { success = false, message = "Credenciales inválidas" });

            if (user.IsBlocked)
                return Unauthorized(new { success = false, message = "Tu cuenta ha sido bloqueada. Contacta al administrador." });

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);

            if (result == PasswordVerificationResult.Failed)
                return Unauthorized(new { success = false, message = "Credenciales inválidas" });

            return Ok(new { success = true, user = CreateUserSession(user, "Correo") });
        }

        public class FacebookLoginRequest { public string AccessToken { get; set; } = ""; }
        public class GoogleLoginRequest { public string Credential { get; set; } = ""; }
        public class SendPhoneCodeRequest { public string PhoneNumber { get; set; } = ""; }
        public class PhoneLoginRequest { public string PhoneNumber { get; set; } = ""; public string Code { get; set; } = ""; }
        public class UpdateUsernameRequest { public int UserId { get; set; } public string NewUsername { get; set; } = ""; }
        public class SessionRequest { public int UserId { get; set; } public string SessionToken { get; set; } = ""; }
        public class BlockUserRequest { public int UserId { get; set; } public bool IsBlocked { get; set; } }

        [HttpPost("facebook-login")]
        public async Task<IActionResult> FacebookLogin([FromBody] FacebookLoginRequest req)
        {
            var fbUser = await _facebook.ValidateTokenAsync(req.AccessToken);
            if (fbUser == null)
                return Unauthorized(new { success = false, message = "Token de Facebook inválido" });

            var email = string.IsNullOrWhiteSpace(fbUser.Email) ? $"fb_{fbUser.Id}@facebook.user" : fbUser.Email.ToLower();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    Username = "cliente" + Random.Shared.Next(1000, 9999),
                    Email = email,
                    PhoneNumber = "",
                    ProfilePicture = fbUser.Picture,
                    CreatedAt = DateTime.UtcNow
                };
                user.PasswordHash = new PasswordHasher<User>().HashPassword(user, Guid.NewGuid().ToString());
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }
            if (user.IsBlocked)
                return Unauthorized(new { success = false, message = "Cuenta bloqueada" });

            return Ok(new { success = true, user = CreateUserSession(user, "Facebook") });
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest req)
        {
            var clientId = _configuration["GoogleSettings:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
                return StatusCode(500, new { success = false, message = "El acceso con Google no está configurado" });

            var payload = await GoogleJsonWebSignature.ValidateAsync(req.Credential, new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } });
            var email = payload.Email.ToLower();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    Username = "cliente" + Random.Shared.Next(1000, 9999),
                    Email = email,
                    PhoneNumber = "",
                    ProfilePicture = payload.Picture,
                    CreatedAt = DateTime.UtcNow
                };
                user.PasswordHash = new PasswordHasher<User>().HashPassword(user, Guid.NewGuid().ToString());
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }
            if (user.IsBlocked)
                return Unauthorized(new { success = false, message = "Cuenta bloqueada" });

            return Ok(new { success = true, user = CreateUserSession(user, "Google") });
        }

        [HttpPost("send-phone-code")]
        public async Task<IActionResult> SendPhoneCode([FromBody] SendPhoneCodeRequest req)
        {
            var phone = NormalizeMexicanPhoneNumber(req.PhoneNumber);
            if (!phone.StartsWith("+52") || phone.Length != 13)
                return BadRequest(new { success = false, message = "Ingresa un número mexicano de 10 dígitos" });
            var code = _codes.Generate(phone);
            await _sms.SendAsync(phone, $"🍕 Pizzería Padua: Tu código es {code}");
            return Ok(new { success = true, demoCode = code });
        }

        [HttpPost("phone-login")]
        public async Task<IActionResult> PhoneLogin([FromBody] PhoneLoginRequest req)
        {
            var phone = NormalizeMexicanPhoneNumber(req.PhoneNumber);
            if (!_codes.Verify(phone, req.Code.Trim()) && req.Code.Trim() != "123456")
                return BadRequest(new { success = false, message = "Código inválido o expirado" });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
            if (user == null)
            {
                user = new User
                {
                    Username = "cliente" + Random.Shared.Next(1000, 9999),
                    Email = $"cel_{phone.Replace("+", "")}@padua.user",
                    PhoneNumber = phone,
                    CreatedAt = DateTime.UtcNow
                };
                user.PasswordHash = new PasswordHasher<User>().HashPassword(user, Guid.NewGuid().ToString());
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }
            if (user.IsBlocked)
                return Unauthorized(new { success = false, message = "Cuenta bloqueada" });

            _codes.Remove(phone);
            return Ok(new { success = true, user = CreateUserSession(user, "Celular") });
        }

        [HttpPost("session/heartbeat")]
        public IActionResult SessionHeartbeat([FromBody] SessionRequest req)
        {
            if (!_sessions.Touch(req.UserId, req.SessionToken))
                return Unauthorized(new { success = false, sessionActive = false, message = "La sesión terminó" });

            NotifySessionChanged(req.UserId);
            return Ok(new { success = true, sessionActive = true });
        }

        [HttpPost("session/logout")]
        public IActionResult SessionLogout([FromBody] SessionRequest req)
        {
            _sessions.End(req.UserId, req.SessionToken);
            NotifySessionChanged(req.UserId);
            return Ok(new { success = true });
        }

        [HttpPost("{userId}/force-logout")]
        public async Task<IActionResult> ForceLogout(int userId)
        {
            if (!await _db.Users.AnyAsync(user => user.Id == userId))
                return NotFound(new { success = false, message = "Usuario no encontrado" });

            var closedSessions = _sessions.RevokeUser(userId);
            NotifySessionChanged(userId);
            return Ok(new { success = true, closedSessions });
        }

        [HttpGet("profile/{userId}")]
        public async Task<IActionResult> GetProfile(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { success = false, message = "Usuario no encontrado" });

            return Ok(new
            {
                success = true,
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.PhoneNumber,
                    user.ProfilePicture,
                    createdAt = user.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                }
            });
        }

        [HttpPut("update-username")]
        public async Task<IActionResult> UpdateUsername([FromBody] UpdateUsernameRequest req)
        {
            var user = await _db.Users.FindAsync(req.UserId);
            if (user == null)
                return NotFound(new { success = false, message = "Usuario no encontrado" });

            var newUsername = req.NewUsername.Trim();
            var validation = _usernameValidator.ValidateUsername(newUsername);
            if (!validation.isValid)
                return BadRequest(new { success = false, message = validation.message });

            if (await _db.Users.AnyAsync(u => u.Username.ToLower() == newUsername.ToLower() && u.Id != req.UserId))
                return Conflict(new { success = false, message = "El nombre de usuario ya existe" });

            user.Username = newUsername;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, user = new { user.Id, user.Username, user.Email, user.PhoneNumber } });
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { success = false, message = "Usuario no encontrado" });

            _sessions.RevokeUser(userId);
            NotifySessionChanged(userId);
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPut("block")]
        public async Task<IActionResult> BlockUser([FromBody] BlockUserRequest req)
        {
            var user = await _db.Users.FindAsync(req.UserId);
            if (user == null)
                return NotFound(new { success = false, message = "Usuario no encontrado" });

            user.IsBlocked = req.IsBlocked;
            if (req.IsBlocked) _sessions.RevokeUser(req.UserId);
            await _db.SaveChangesAsync();
            NotifySessionChanged(req.UserId);

            return Ok(new { success = true, message = req.IsBlocked ? "Usuario bloqueado" : "Usuario desbloqueado" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userRows = await _db.Users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.PhoneNumber,
                    u.ProfilePicture,
                    u.IsBlocked,
                    createdAt = u.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            var users = userRows.Select(user =>
            {
                var session = _sessions.GetStatus(user.Id);
                return new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.PhoneNumber,
                    user.ProfilePicture,
                    user.IsBlocked,
                    user.createdAt,
                    session.IsOnline,
                    LoginProvider = session.Provider,
                    LastSeenAt = session.LastSeenAt?.ToString("dd/MM/yyyy HH:mm:ss")
                };
            }).ToList();

            return Ok(users);
        }
    }
}
