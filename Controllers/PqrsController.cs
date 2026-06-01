using asp_backend.Data;
using asp_backend.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asp_backend.Controllers;

[ApiController]
[Route("pqrs")]
[Produces("application/json")]
[Tags("PQRS")]
[AllowAnonymous]
public class PqrsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PqrsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PqrsResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePqrsRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Subject and message are required." });
        }

        var user = await FindOrCreateUserAsync(request.Email, request.FullName);
        var pqr = new Pqr
        {
            UserId = user.Id,
            Type = string.IsNullOrWhiteSpace(request.Type) ? "general" : request.Type.Trim(),
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            Status = "pending",
            CreatedAt = DbTimeNow()
        };

        _db.Pqrs.Add(pqr);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToDto(pqr));
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(List<PqrsResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine([FromQuery] Guid? userId = null, [FromQuery] string? email = null)
    {
        var query = _db.Pqrs
            .AsNoTracking()
            .Include(pqr => pqr.User)
            .Include(pqr => pqr.PqrsResponses)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(pqr => pqr.UserId == userId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            query = query.Where(pqr => pqr.User != null && pqr.User.Email.ToLower() == normalizedEmail);
        }
        else
        {
            return Ok(new List<PqrsResponseDto>());
        }

        var items = await query
            .OrderByDescending(pqr => pqr.CreatedAt)
            .Take(50)
            .ToListAsync();

        return Ok(items.Select(ToDto));
    }

    private async Task<User> FindOrCreateUserAsync(string email, string? fullName)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(item => item.Email.ToLower() == normalizedEmail);
        if (user != null)
        {
            return user;
        }

        user = new User
        {
            Email = normalizedEmail,
            FullName = string.IsNullOrWhiteSpace(fullName) ? normalizedEmail : fullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
            CreatedAt = DbTimeNow()
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private static PqrsResponseDto ToDto(Pqr pqr)
    {
        return new PqrsResponseDto(
            pqr.Id,
            pqr.UserId,
            pqr.Type,
            pqr.Subject,
            pqr.Message,
            pqr.Status,
            pqr.CreatedAt,
            pqr.PqrsResponses
                .OrderBy(response => response.CreatedAt)
                .Select(response => new PqrsAnswerDto(response.Id, response.Response, response.CreatedAt))
                .ToList()
        );
    }

    private static DateTime DbTimeNow()
    {
        return DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
    }
}

public class CreatePqrsRequest
{
    public Guid? UserId { get; init; }
    public string? Email { get; init; }
    public string? FullName { get; init; }
    public string? Type { get; init; }
    public string? Subject { get; init; }
    public string? Message { get; init; }
}

public record PqrsResponseDto(
    Guid Id,
    Guid? UserId,
    string? Type,
    string? Subject,
    string? Message,
    string? Status,
    DateTime? CreatedAt,
    List<PqrsAnswerDto> Responses
);

public record PqrsAnswerDto(Guid Id, string? Response, DateTime? CreatedAt);
