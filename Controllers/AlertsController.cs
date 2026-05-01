using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTracker.API.Data;
using StockTracker.API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace StockTracker.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AlertsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AlertsController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var userId) ? userId : 0;
        }

        // GET: api/Alerts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Alert>>> GetMyAlerts()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            return await _context.Alerts
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        // GET: api/Alerts/triggered — Tetiklenen alertler
        [HttpGet("triggered")]
        public async Task<ActionResult> GetTriggeredAlerts()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var triggered = await _context.Alerts
                .Where(a => a.UserId == userId && !a.IsActive && a.TriggeredAt.HasValue)
                .OrderByDescending(a => a.TriggeredAt)
                .ToListAsync();

            return Ok(triggered);
        }

        // POST: api/Alerts
        [HttpPost]
        public async Task<ActionResult<Alert>> PostAlert(Alert alert)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            alert.UserId = userId;
            alert.IsActive = true;
            alert.CreatedAt = DateTime.UtcNow;

            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMyAlerts), alert);
        }

        // DELETE: api/Alerts/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlert(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var alert = await _context.Alerts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (alert == null) return NotFound();

            _context.Alerts.Remove(alert);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/Alerts/{id}/reactivate — Aleti tekrar aktif et
        [HttpPut("{id}/reactivate")]
        public async Task<IActionResult> ReactivateAlert(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var alert = await _context.Alerts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (alert == null) return NotFound();

            alert.IsActive = true;
            alert.TriggeredAt = null;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
