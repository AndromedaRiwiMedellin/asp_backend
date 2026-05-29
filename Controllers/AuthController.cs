using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using asp_backend.Data;
using asp_backend.models;

namespace asp_backend.Controllers;

/// <summary>
/// Endpoints for authentication and user access.
/// </summary>
[ApiController]
[Route("auth")]
[Produces("application/json")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuthController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Authenticates a POS user.
    /// </summary>
    [HttpPost("pos-login")]
    public async Task<IActionResult> PosLogin([FromBody] LoginRequest request)
    {
        if (request == null)
            return BadRequest(new { message = "Request body is required." });

        var user = await _db.Users
            .Include(u => u.Employees)
                .ThenInclude(e => e.Permissions)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null)
            return Unauthorized(new { message = "Invalid credentials" });

        var validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!validPassword)
            return Unauthorized(new { message = "Invalid credentials" });

        var hasPermission = user.Employees.Any(e => e.Permissions.Any(p => p.Id == 1));
        if (!hasPermission)
            return Unauthorized(new { message = "No tienes permiso para acceder al POS." });

        return Ok(new { userId = user.Id, email = user.Email, fullName = user.FullName, message = "Login POS ok" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null)
            return BadRequest(new { message = "Request body is required." });

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null)
            return Unauthorized(new { message = "Invalid credentials" });

        var validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!validPassword)
            return Unauthorized(new { message = "Invalid credentials" });

        return Ok(new LoginResponse(user.Id, user.Email, "login ok"));
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <remarks>
    /// Creates a new user with email and password. Email must be unique (case-insensitive)
    /// and password must be at least 8 characters.
    /// </remarks>
    /// <param name="request">Registration data.</param>
    /// <response code="201">User created successfully.</response>
    /// <response code="409">Email is already in use.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request == null)
            return BadRequest(new { message = "Request body is required." });

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email is required." });

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return BadRequest(new { message = "Password must be at least 8 characters." });

        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { message = "Full name is required." });

        var emailExists = await _db.Users
            .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (emailExists)
            return Conflict(new { message = "Email is already in use." });

        var user = new User
        {
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            CreatedAt = DateTime.Now
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Register), new RegisterResponse(user.Id, user.Email, "Account created successfully."));
    }

    [HttpGet("check-email")]
    public async Task<IActionResult> CheckEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return BadRequest();
        
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        if (user != null)
        {
            return Ok(new { exists = true, fullName = user.FullName });
        }
        return Ok(new { exists = false });
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), Email = "juan@correo.com", FullName = "Juan Perez", PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678"), CreatedAt = DateTime.Now },
            new User { Id = Guid.NewGuid(), Email = "maria@correo.com", FullName = "Maria Gomez", PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678"), CreatedAt = DateTime.Now },
            new User { Id = Guid.NewGuid(), Email = "carlos@correo.com", FullName = "Carlos Ruiz", PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678"), CreatedAt = DateTime.Now },
            new User { Id = Guid.NewGuid(), Email = "ana@correo.com", FullName = "Ana Lopez", PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678"), CreatedAt = DateTime.Now }
        };

        foreach (var user in users)
        {
            if (!await _db.Users.AnyAsync(u => u.Email == user.Email))
            {
                _db.Users.Add(user);
            }
        }
        await _db.SaveChangesAsync();

        return Ok(new { message = "4 Users seeded successfully." });
    }
}

/// <summary>Request payload for login.</summary>
public class LoginRequest
{
    /// <summary>Email address.</summary>
    public string Email { get; init; } = string.Empty;
    /// <summary>Password.</summary>
    public string Password { get; init; } = string.Empty;
}

/// <summary>Login response payload.</summary>
public record LoginResponse(Guid UserId, string Email, string Message);

/// <summary>Request payload for registration.</summary>
public class RegisterRequest
{
    /// <summary>Email address.</summary>
    public string Email { get; init; } = string.Empty;
    /// <summary>Password (minimum 8 characters).</summary>
    public string Password { get; init; } = string.Empty;
    /// <summary>Full name of the user.</summary>
    public string FullName { get; init; } = string.Empty;
}

/// <summary>Registration response payload.</summary>
public record RegisterResponse(Guid UserId, string Email, string Message);