using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OdooBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly OdooDBContext _context;

        public AuthController(OdooDBContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        { 
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == request.Username && u.Password == request.Password && u.Location==request.Location);

                if (user == null || user.Type != 1)
                    return Unauthorized(new { success = false, message = "Invalid credentials or not a manager." });

                return Ok(new
                {
                    success = true,
                    user.Id,
                    user.UserName,
                    user.Location
                }); 
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;
    }

}

