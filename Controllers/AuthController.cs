using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using asp_backend.Data;

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
    /// <remarks>
    /// Validates the provided credentials against the database and returns a session payload
    /// containing the authenticated user identifier and email address.
    /// </remarks>
    /// <param name="request">Credentials used to sign in.</param>
    /// <response code="200">Authentication succeeded. Returns the authenticated user information.</response>
    /// <response code="401">The credentials are invalid.</response>
    /// <response code="400">The request body is missing or malformed.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!validPassword)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        return Ok(new LoginResponse(user.Id, user.Email, "login ok"));
    }
}

/// <summary>
/// Request payload used to authenticate a user.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Email address associated with the account.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Password provided by the user.
    /// </summary>
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Authentication response returned after a successful login.
/// </summary>
public record LoginResponse(int UserId, string Email, string Message);
