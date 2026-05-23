using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using asp_backend.Data;

namespace asp_backend.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuthController(AppDbContext db)
    {
        _db = db;
    }
    
    /// <summary>
    ///Authenticate a user and return their information
    /// </summary>
    /// <remarks>
    /// Validate the email and password against the database.
    /// Returns 401 if the credentials are invalid.
    /// </remarks>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return Unauthorized(new { message = "Invalid credentials" });

        var validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!validPassword)
            return Unauthorized(new { message = "Invalid credentials"  });

        return Ok(new { message = "login ok", userId = user.Id, email = user.Email });
    }
}

public record LoginRequest(string Email, string Password);