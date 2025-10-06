using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTracker.API.Data;
using StockTracker.API.DTOs;
using StockTracker.API.Models;
using StockTracker.API.Services;

namespace StockTracker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;

        public AuthController(AppDbContext context, AuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        {
            // Check if user exists
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest("Email already exists");
            }

            // Create user
            var user = new User
            {
                Email = dto.Email,
                PasswordHash = _authService.HashPassword(dto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate token
            var token = _authService.GenerateJwtToken(user.Id, user.Email);

            return Ok(new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Token = token
            });
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !_authService.VerifyPassword(dto.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid email or password");
            }

            var token = _authService.GenerateJwtToken(user.Id, user.Email);

            return Ok(new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Token = token
            });
        }
    }
}