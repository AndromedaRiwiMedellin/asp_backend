using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using asp_backend.Data;
using asp_backend.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace asp_backend.Controllers;

[ApiController]
[Route("auth")]
[Produces("application/json")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var user = await _db.Users
            .Include(u => u.Employees)
            .ThenInclude(e => e.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!validPassword)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var employee = user.Employees.FirstOrDefault(e => e.Active == true);
        var role = employee?.Role?.Name ?? "user";

        var token = GenerateToken(user, role, employee?.Id);
        var expiresAt = DateTime.UtcNow.AddHours(8);
        var activeEvent = await ResolveActiveEventAsync();

        return Ok(new LoginResponse(
            token,
            expiresAt,
            user.Id,
            employee?.Id,
            role,
            user.Email,
            user.FullName ?? user.Email,
            activeEvent,
            "login ok"
        ));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me()
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid token." });
        }

        var user = await _db.Users
            .Include(u => u.Employees)
            .ThenInclude(e => e.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        var employee = user.Employees.FirstOrDefault(e => e.Active == true);
        var activeEvent = await ResolveActiveEventAsync();

        return Ok(new CurrentUserResponse(
            user.Id,
            employee?.Id,
            user.Email,
            user.FullName ?? user.Email,
            employee?.Role?.Name ?? "user",
            activeEvent
        ));
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return BadRequest(new { message = "Password must be at least 8 characters." });
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new { message = "Full name is required." });
        }

        var emailExists = await _db.Users
            .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (emailExists)
        {
            return Conflict(new { message = "Email is already in use." });
        }

        var user = new User
        {
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            CreatedAt = DbTimeNow()
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Register), new RegisterResponse(user.Id, user.Email, "Account created successfully."));
    }

    private string GenerateToken(User user, string role, Guid? employeeId)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"] ?? "andromeda-api";
        var audience = jwtSection["Audience"] ?? "andromeda-client";
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("JWT key is missing from configuration.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, role)
        };

        if (employeeId.HasValue)
        {
            claims.Add(new Claim("employeeId", employeeId.Value.ToString()));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<EventContext?> ResolveActiveEventAsync()
    {
        var now = DbTimeNow();

        var upcoming = await _db.Events
            .AsNoTracking()
            .Where(e => e.EventDate.HasValue && e.EventDate.Value >= now)
            .OrderBy(e => e.EventDate)
            .Select(e => new EventContext(e.Id, e.Title, e.EventDate))
            .FirstOrDefaultAsync();

        if (upcoming != null)
        {
            return upcoming;
        }

        return await _db.Events
            .AsNoTracking()
            .OrderByDescending(e => e.EventDate)
            .Select(e => new EventContext(e.Id, e.Title, e.EventDate))
            .FirstOrDefaultAsync();
    }

    private static DateTime DbTimeNow()
    {
        return DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
    }
}

public class LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    Guid UserId,
    Guid? EmployeeId,
    string Role,
    string Email,
    string FullName,
    EventContext? ActiveEvent,
    string Message
);

public class RegisterRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
}

public record RegisterResponse(Guid UserId, string Email, string Message);

public record CurrentUserResponse(
    Guid UserId,
    Guid? EmployeeId,
    string Email,
    string FullName,
    string Role,
    EventContext? ActiveEvent
);
