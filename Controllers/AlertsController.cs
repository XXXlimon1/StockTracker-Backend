using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTracker.API.Data;
using StockTracker.API.Models;
using Microsoft.AspNetCore.Authorization;

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

        // GET: api/Alerts/user/5
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Alert>>> GetUserAlerts(int userId)
        {
            return await _context.Alerts
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        // POST: api/Alerts
        [HttpPost]
        public async Task<ActionResult<Alert>> PostAlert(Alert alert)
        {
            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserAlerts), new { userId = alert.UserId }, alert);
        }

        // PUT: api/Alerts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAlert(int id, Alert alert)
        {
            if (id != alert.Id)
            {
                return BadRequest();
            }

            _context.Entry(alert).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Alerts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlert(int id)
        {
            var alert = await _context.Alerts.FindAsync(id);
            if (alert == null)
            {
                return NotFound();
            }

            _context.Alerts.Remove(alert);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}