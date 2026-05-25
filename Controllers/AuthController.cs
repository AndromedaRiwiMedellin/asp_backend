using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using asp_backend.Data;
using asp_backend.Models;

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
    /// Authenticates a user with email and password.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Register), new RegisterResponse(user.Id, user.Email, "Account created successfully."));
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
public record LoginResponse(int UserId, string Email, string Message);

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
public record RegisterResponse(int UserId, string Email, string Message);