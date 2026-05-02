using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTracker.API.Data;
using System.Security.Claims;

namespace StockTracker.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var userId) ? userId : 0;
        }

        // GET: api/Users/me
        [HttpGet("me")]
        public async Task<ActionResult> GetMe()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return Ok(new { id = user.Id, email = user.Email });
        }

        // PUT: api/Users/fcm-token — FCM token kaydet
        [HttpPut("fcm-token")]
        public async Task<IActionResult> UpdateFcmToken([FromBody] FcmTokenRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.FcmToken = request.Token;
            await _context.SaveChangesAsync();

            return Ok();
        }
    }

    public class FcmTokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
