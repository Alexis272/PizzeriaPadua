using PizzeriaPadua.Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzeriaPadua.API.Data;
using PizzeriaPadua.API.Services;

namespace PizzeriaPadua.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly PizzeriaPaduaContext _db;
        private readonly UsernameValidationService _validator;
        private readonly ILogger<ReviewsController> _log;

        public ReviewsController(PizzeriaPaduaContext db, UsernameValidationService validator, ILogger<ReviewsController> log)
        {
            _db = db;
            _validator = validator;
            _log = log;
        }

        public class CreateReviewRequest
        {
            public int UserId { get; set; }
            public string Comment { get; set; } = "";
            public int Rating { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var reviews = await _db.Reviews
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.UserId,
                    r.Username,
                    r.Comment,
                    r.Rating,
                    createdAt = r.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReviewRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Comment))
                return BadRequest(new { success = false, message = "El comentario no puede estar vacío" });

            if (req.Rating < 1 || req.Rating > 5)
                return BadRequest(new { success = false, message = "La calificación debe ser entre 1 y 5" });

            var user = await _db.Users.FindAsync(req.UserId);
            if (user == null)
                return NotFound(new { success = false, message = "Usuario no encontrado" });

            var validation = _validator.ValidateText(req.Comment, "comentario");
            if (!validation.isValid)
                return BadRequest(new { success = false, message = validation.message });

            var review = new Review
            {
                UserId = req.UserId,
                Username = user.Username,
                Comment = req.Comment.Trim(),
                Rating = req.Rating,
                CreatedAt = DateTime.UtcNow
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                review = new
                {
                    review.Id,
                    review.UserId,
                    review.Username,
                    review.Comment,
                    review.Rating,
                    createdAt = review.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                }
            });
        }

        [HttpGet("average")]
        public async Task<IActionResult> GetAverage()
        {
            var reviews = await _db.Reviews.ToListAsync();
            if (reviews.Count == 0)
                return Ok(new { average = 0, count = 0 });

            var average = reviews.Average(r => r.Rating);
            return Ok(new { average = Math.Round(average, 1), count = reviews.Count });
        }
    }
}
