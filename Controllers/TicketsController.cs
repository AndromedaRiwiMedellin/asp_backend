using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using asp_backend.Data;
using asp_backend.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asp_backend.Controllers;

[ApiController]
[Route("tickets")]
[Produces("application/json")]
[Tags("Tickets")]
[Authorize(Roles = "employee,admin,superadmin")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TicketsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("events")]
    [ProducesResponseType(typeof(List<EventOption>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _db.Events
            .AsNoTracking()
            .OrderByDescending(e => e.EventDate)
            .Select(e => new EventOption(e.Id, e.Title, e.EventDate))
            .ToListAsync();

        return Ok(events);
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(EmployeeDashboardResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard([FromQuery] Guid? eventId = null)
    {
        var employeeId = await ResolveEmployeeIdAsync();
        var session = await BuildSessionContextAsync(eventId, employeeId);
        if (IsReadOnlyRole(session.Role))
        {
            return Ok(new EmployeeDashboardResponse(
                new EmployeeDashboardStats(0, 0, 0, 0, 0),
                [],
                session
            ));
        }

        var scansQuery = _db.TicketScans
            .AsNoTracking()
            .Include(s => s.Ticket)
            .ThenInclude(t => t!.User)
            .Include(s => s.Ticket)
            .ThenInclude(t => t!.Event)
            .AsQueryable();

        if (employeeId.HasValue)
        {
            scansQuery = scansQuery.Where(s => s.ScannedBy == employeeId.Value);
        }

        if (session.ActiveEvent != null)
        {
            scansQuery = scansQuery.Where(s => 
                s.Ticket == null || 
                s.Ticket.EventId == session.ActiveEvent.EventId || 
                s.Reason == "WRONG_EVENT"
            );
        }

        var scans = await scansQuery
            .OrderByDescending(s => s.ScannedAt)
            .Take(50)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var scannedToday = scans.Count(s => s.ScannedAt.HasValue && s.ScannedAt.Value.Date == today);
        var validCount = scans.Count(s => s.Success == true);
        var fraudCount = scans.Count(s => IsFraud(s));
        var illegalCount = scans.Count(s => IsIllegal(s));

        var recent = scans.Select(scan =>
        {
            var ticket = scan.Ticket;
            return new TicketScanItem(
                scan.Id,
                scan.ScannedAt,
                scan.Success == true,
                NormalizeReason(scan.Reason),
                ticket?.QrCode,
                ticket?.Status,
                ticket?.User?.FullName ?? ticket?.User?.Email,
                ticket?.User?.Email,
                ticket?.Event?.Title,
                ticket?.SeatNumber
            );
        }).ToList();

        return Ok(new EmployeeDashboardResponse(
            new EmployeeDashboardStats(
                TotalScans: scans.Count,
                ScannedToday: scannedToday,
                ValidScans: validCount,
                FraudScans: fraudCount,
                IllegalScans: illegalCount
            ),
            recent,
            session
        ));
    }

    [HttpPost("scan")]
    [ProducesResponseType(typeof(TicketScanResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScanTicket([FromBody] ScanTicketRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.QrCode))
        {
            return BadRequest(new { message = "qrCode is required." });
        }

        var employeeId = await ResolveEmployeeIdAsync();
        var session = await BuildSessionContextAsync(request.EventId, employeeId);
        if (IsReadOnlyRole(session.Role))
        {
            return Ok(new TicketScanResultResponse(
                false,
                "Rol superadmin en modo lectura. Sin escaneo por ahora.",
                "READ_ONLY_ROLE",
                null,
                session
            ));
        }

        var qrCode = request.QrCode.Trim();

        var ticket = await _db.Tickets
            .Include(t => t.User)
            .Include(t => t.Event)
            .FirstOrDefaultAsync(t => t.QrCode == qrCode);

        if (ticket == null)
        {
            var unknownScan = new TicketScan
            {
                ScannedBy = employeeId,
                Success = false,
                Reason = "NOT_FOUND",
                ScannedAt = DbTimeNow()
            };

            _db.TicketScans.Add(unknownScan);
            await _db.SaveChangesAsync();

            return Ok(new TicketScanResultResponse(
                false,
                "Ticket no existe. Posible fraude.",
                "NOT_FOUND",
                null,
                session
            ));
        }

        if (session.ActiveEvent != null && ticket.EventId != session.ActiveEvent.EventId)
        {
            var wrongEventScan = new TicketScan
            {
                TicketId = ticket.Id,
                ScannedBy = employeeId,
                Success = false,
                Reason = "WRONG_EVENT",
                ScannedAt = DbTimeNow()
            };

            _db.TicketScans.Add(wrongEventScan);
            await _db.SaveChangesAsync();

            return Ok(new TicketScanResultResponse(
                false,
                "El ticket pertenece a otro evento.",
                "WRONG_EVENT",
                new TicketDetails(
                    ticket.Id,
                    ticket.QrCode,
                    ticket.Status,
                    ticket.User?.FullName ?? ticket.User?.Email,
                    ticket.User?.Email,
                    ticket.Event?.Title,
                    ticket.SeatNumber
                ),
                session
            ));
        }

        var normalizedStatus = (ticket.Status ?? "VALID").ToUpperInvariant();
        var success = false;
        var reason = "INVALID";
        var message = "Ticket inválido.";

        if (normalizedStatus == "VALID")
        {
            success = true;
            reason = "VALID";
            message = "Ticket válido. Ingreso autorizado.";
            ticket.Status = "USED";
        }
        else if (normalizedStatus == "USED")
        {
            reason = "ALREADY_USED";
            message = "Ticket ya fue usado.";
        }
        else if (normalizedStatus == "FRAUD")
        {
            reason = "FRAUD";
            message = "Ticket marcado como fraude.";
        }
        else if (normalizedStatus == "ILLEGAL")
        {
            reason = "ILLEGAL";
            message = "Ticket ilegal. Retener y reportar.";
        }
        else
        {
            reason = normalizedStatus;
            message = $"Ticket no autorizado: {normalizedStatus}.";
        }

        var scan = new TicketScan
        {
            TicketId = ticket.Id,
            ScannedBy = employeeId,
            Success = success,
            Reason = reason,
            ScannedAt = DbTimeNow()
        };

        _db.TicketScans.Add(scan);
        await _db.SaveChangesAsync();

        return Ok(new TicketScanResultResponse(
            success,
            message,
            reason,
            new TicketDetails(
                ticket.Id,
                ticket.QrCode,
                ticket.Status,
                ticket.User?.FullName ?? ticket.User?.Email,
                ticket.User?.Email,
                ticket.Event?.Title,
                ticket.SeatNumber
            ),
            session
        ));
    }

    private async Task<Guid?> ResolveEmployeeIdAsync()
    {
        var employeeClaim = User.FindFirstValue("employeeId");
        if (Guid.TryParse(employeeClaim, out var employeeIdFromClaim))
        {
            return employeeIdFromClaim;
        }

        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return await _db.Employees
            .Where(e => e.UserId == userId && e.Active == true)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<ScannerSession> BuildSessionContextAsync(Guid? requestedEventId, Guid? employeeId)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "user";
        var userEmail = User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "unknown";
        var activeEvent = await ResolveActiveEventAsync(requestedEventId);

        return new ScannerSession(userEmail, role, employeeId, activeEvent);
    }

    private async Task<EventContext?> ResolveActiveEventAsync(Guid? requestedEventId)
    {
        if (requestedEventId.HasValue)
        {
            var requested = await _db.Events
                .AsNoTracking()
                .Where(e => e.Id == requestedEventId.Value)
                .Select(e => new EventContext(e.Id, e.Title, e.EventDate))
                .FirstOrDefaultAsync();

            if (requested != null)
            {
                return requested;
            }
        }

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

    private static bool IsFraud(TicketScan scan)
    {
        var reason = NormalizeReason(scan.Reason);
        return reason is "FRAUD" or "NOT_FOUND";
    }

    private static bool IsIllegal(TicketScan scan)
    {
        return NormalizeReason(scan.Reason) == "ILLEGAL";
    }

    private static string NormalizeReason(string? reason)
    {
        return string.IsNullOrWhiteSpace(reason)
            ? "UNKNOWN"
            : reason.Trim().ToUpperInvariant();
    }

    private static DateTime DbTimeNow()
    {
        return DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
    }

    private static bool IsReadOnlyRole(string role)
    {
        return role.Equals("superadmin", StringComparison.OrdinalIgnoreCase);
    }
}

public class ScanTicketRequest
{
    public string QrCode { get; init; } = string.Empty;
    public Guid? EventId { get; init; }
}

public record EmployeeDashboardResponse(EmployeeDashboardStats Stats, List<TicketScanItem> RecentScans, ScannerSession Session);

public record EmployeeDashboardStats(int TotalScans, int ScannedToday, int ValidScans, int FraudScans, int IllegalScans);

public record TicketScanItem(
    Guid ScanId,
    DateTime? ScannedAt,
    bool Success,
    string Reason,
    string? QrCode,
    string? TicketStatus,
    string? OwnerName,
    string? OwnerEmail,
    string? EventTitle,
    string? SeatNumber
);

public record TicketScanResultResponse(bool Success, string Message, string Reason, TicketDetails? Ticket, ScannerSession Session);

public record TicketDetails(
    Guid TicketId,
    string QrCode,
    string? Status,
    string? OwnerName,
    string? OwnerEmail,
    string? EventTitle,
    string? SeatNumber
);

public record EventOption(Guid EventId, string EventTitle, DateTime? EventDate);

public record ScannerSession(string UserEmail, string Role, Guid? EmployeeId, EventContext? ActiveEvent);
