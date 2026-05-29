using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using asp_backend.Data;
using asp_backend.models;

namespace asp_backend.Controllers;

[ApiController]
[Route("pqrs")]
[Produces("application/json")]
[Tags("PQRS")]
public class PqrsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PqrsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePqrsRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Type) || string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Type, subject and message are required." });
        }

        var user = await ResolveUserAsync(request.UserId, request.Email);
        if (user == null)
        {
            return BadRequest(new { message = "A valid user is required to create a PQRS request." });
        }

        var pqrs = new Pqr
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = request.Type.Trim(),
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            Status = "OPEN",
            CreatedAt = DbNow()
        };

        _db.Pqrs.Add(pqrs);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = pqrs.Id }, ToResponse(pqrs, user));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null)
    {
        var query = _db.Pqrs
            .AsNoTracking()
            .Include(pqrs => pqrs.User)
            .Include(pqrs => pqrs.PqrsResponses)
                .ThenInclude(response => response.Employee)
                    .ThenInclude(employee => employee!.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(pqrs => pqrs.Status != null && pqrs.Status.ToUpper() == normalizedStatus);
        }

        var items = await query
            .OrderByDescending(pqrs => pqrs.CreatedAt)
            .ToListAsync();

        return Ok(items.Select(pqrs => ToResponse(pqrs, pqrs.User)).ToList());
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMine([FromQuery] Guid? userId = null, [FromQuery] string? email = null)
    {
        var user = await ResolveUserAsync(userId, email);
        if (user == null)
        {
            return BadRequest(new { message = "A valid user is required." });
        }

        var items = await _db.Pqrs
            .AsNoTracking()
            .Include(pqrs => pqrs.User)
            .Include(pqrs => pqrs.PqrsResponses)
                .ThenInclude(response => response.Employee)
                    .ThenInclude(employee => employee!.User)
            .Where(pqrs => pqrs.UserId == user.Id)
            .OrderByDescending(pqrs => pqrs.CreatedAt)
            .ToListAsync();

        return Ok(items.Select(pqrs => ToResponse(pqrs, pqrs.User)).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var pqrs = await _db.Pqrs
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.PqrsResponses)
                .ThenInclude(response => response.Employee)
                    .ThenInclude(employee => employee!.User)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (pqrs == null)
        {
            return NotFound(new { message = "PQRS request not found." });
        }

        return Ok(ToResponse(pqrs, pqrs.User));
    }

    [HttpPost("{id:guid}/responses")]
    public async Task<IActionResult> Respond(Guid id, [FromBody] CreatePqrsResponseRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Response))
        {
            return BadRequest(new { message = "Response is required." });
        }

        var pqrs = await _db.Pqrs.FirstOrDefaultAsync(item => item.Id == id);
        if (pqrs == null)
        {
            return NotFound(new { message = "PQRS request not found." });
        }

        if (request.EmployeeId.HasValue)
        {
            var employeeExists = await _db.Employees.AnyAsync(employee => employee.Id == request.EmployeeId.Value);
            if (!employeeExists)
            {
                return BadRequest(new { message = "Employee not found." });
            }
        }

        var response = new PqrsResponse
        {
            Id = Guid.NewGuid(),
            PqrsId = pqrs.Id,
            EmployeeId = request.EmployeeId,
            Response = request.Response.Trim(),
            CreatedAt = DbNow()
        };

        pqrs.Status = string.IsNullOrWhiteSpace(request.Status) ? "ANSWERED" : request.Status.Trim().ToUpperInvariant();
        _db.PqrsResponses.Add(response);
        await _db.SaveChangesAsync();

        return Ok(new { message = "PQRS response saved.", pqrsId = pqrs.Id, responseId = response.Id, status = pqrs.Status });
    }

    private async Task<User?> ResolveUserAsync(Guid? userId, string? email)
    {
        if (userId.HasValue)
        {
            return await _db.Users.FirstOrDefaultAsync(user => user.Id == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return await _db.Users.FirstOrDefaultAsync(user => user.Email.ToLower() == normalizedEmail);
        }

        return null;
    }

    private static PqrsItemResponse ToResponse(Pqr pqrs, User? user)
    {
        return new PqrsItemResponse(
            pqrs.Id,
            pqrs.Type,
            pqrs.Subject,
            pqrs.Message,
            pqrs.Status ?? "OPEN",
            pqrs.CreatedAt,
            user == null ? null : new PqrsUserResponse(user.Id, user.FullName, user.Email),
            pqrs.PqrsResponses
                .OrderBy(response => response.CreatedAt)
                .Select(response => new PqrsAnswerResponse(
                    response.Id,
                    response.Response,
                    response.CreatedAt,
                    response.Employee?.User?.FullName ?? response.Employee?.User?.Email
                ))
                .ToList()
        );
    }

    private static DateTime DbNow() => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
}

public class CreatePqrsRequest
{
    public Guid? UserId { get; init; }
    public string? Email { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public class CreatePqrsResponseRequest
{
    public Guid? EmployeeId { get; init; }
    public string Response { get; init; } = string.Empty;
    public string? Status { get; init; }
}

public record PqrsItemResponse(
    Guid Id,
    string? Type,
    string? Subject,
    string? Message,
    string Status,
    DateTime? CreatedAt,
    PqrsUserResponse? User,
    List<PqrsAnswerResponse> Responses
);

public record PqrsUserResponse(Guid Id, string? FullName, string Email);

public record PqrsAnswerResponse(Guid Id, string? Response, DateTime? CreatedAt, string? EmployeeName);
