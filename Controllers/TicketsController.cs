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
[Authorize]
// [Authorize(Roles = "employee,admin,superadmin")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TicketsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TicketPrintResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicket(Guid id)
    {
        var ticket = await _db.Tickets
            .AsNoTracking()
            .Include(t => t.Event)
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return NotFound(new { message = "Ticket not found" });
        }

        return Ok(ToTicketPrintResponse(ticket));
    }

    [HttpPost("{id:guid}/print")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TicketPrintResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PrintTicket(Guid id)
    {
        var result = await GetTicket(id);
        return result;
    }

    [HttpGet("purchases")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<PurchaseHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchases([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Ok(new List<PurchaseHistoryResponse>());
        }

        var tickets = await _db.Tickets
            .AsNoTracking()
            .Include(t => t.Event)
            .Include(t => t.User)
            .Where(t => t.User != null && t.User.Email.ToLower() == email.ToLower())
            .OrderByDescending(t => t.PurchasedAt)
            .ToListAsync();

        var areaLookup = await BuildAreaLookupAsync(tickets.Select(t => t.Id));

        var purchases = tickets
            .GroupBy(t => new
            {
                t.EventId,
                PurchasedAt = t.PurchasedAt?.ToString("O") ?? string.Empty
            })
            .Select(group =>
            {
                var first = group.First();
                var items = group.ToList();
                var zone = items
                    .Select(t => areaLookup.GetValueOrDefault(t.Id))
                    .FirstOrDefault(a => a != null);
                var total = items.Sum(t => areaLookup.GetValueOrDefault(t.Id)?.Price ?? 0);

                return new PurchaseHistoryResponse(
                    first.Id,
                    first.Event,
                    zone == null ? null : new PurchaseZoneResponse(zone.Id, zone.AreaName, zone.Price),
                    items.Select(ToTicketItemResponse).ToList(),
                    total,
                    "Paid",
                    first.PurchasedAt
                );
            })
            .ToList();

        return Ok(purchases);
    }

    [HttpPost("purchase-pos")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PurchasePosResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PurchasePos([FromBody] PurchasePosRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Customer email is required." });
        }

        if (request.EventId == Guid.Empty || request.AreaId <= 0 || request.Seats.Count == 0)
        {
            return BadRequest(new { message = "Event, area and at least one seat are required." });
        }

        var ev = await _db.Events.FindAsync(request.EventId);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        var area = await _db.EventAreas
            .Include(a => a.AreaSeats)
            .FirstOrDefaultAsync(a => a.Id == request.AreaId && a.EventId == request.EventId);

        if (area == null)
        {
            return NotFound(new { message = "Event area not found." });
        }

        var requestedSeats = request.Seats
            .Where(seat => !string.IsNullOrWhiteSpace(seat))
            .Select(seat => seat.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var seats = area.AreaSeats
            .Where(seat => requestedSeats.Contains(seat.SeatNumber, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (seats.Count != requestedSeats.Count)
        {
            return BadRequest(new { message = "One or more seats do not exist." });
        }

        if (seats.Any(seat => !IsSeatAvailable(seat.Status)))
        {
            return Conflict(new { message = "One or more seats are not available." });
        }

        var user = await FindOrCreateCustomerAsync(request.Email, request.FullName, request.Phone);
        var now = DbTimeNow();
        var createdTickets = new List<Ticket>();

        foreach (var seat in seats)
        {
            var ticket = new Ticket
            {
                UserId = user.Id,
                EventId = ev.Id,
                SellerId = request.SellerId,
                QrCode = $"ANDRO-{Guid.NewGuid():N}".ToUpperInvariant(),
                SeatNumber = seat.SeatNumber,
                Status = "VALID",
                PurchasedAt = now
            };

            _db.Tickets.Add(ticket);
            createdTickets.Add(ticket);

            seat.UserId = user.Id;
            seat.Ticket = ticket;
            seat.Status = "sold";
            seat.SoldAt = now;
            seat.UpdatedAt = now;
        }

        await _db.SaveChangesAsync();

        return Ok(new PurchasePosResponse(
            createdTickets.Select(ToTicketItemResponse).ToList(),
            createdTickets.Count,
            createdTickets.Count * area.Price,
            "Purchase completed."
        ));
    }

    [HttpGet("daily-sales")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DailySalesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDailySales([FromQuery] Guid? sellerId = null, [FromQuery] DateTime? date = null)
    {
        var start = DateTime.SpecifyKind((date ?? DateTime.UtcNow).Date, DateTimeKind.Unspecified);
        var end = start.AddDays(1);

        var query = _db.Tickets
            .AsNoTracking()
            .Include(t => t.Event)
            .Include(t => t.User)
            .Where(t => t.PurchasedAt >= start && t.PurchasedAt < end);

        if (sellerId.HasValue)
        {
            query = query.Where(t => t.SellerId == sellerId.Value);
        }

        var tickets = await query
            .OrderByDescending(t => t.PurchasedAt)
            .ToListAsync();

        var areaLookup = await BuildAreaLookupAsync(tickets.Select(t => t.Id));
        var activity = tickets.Select(ticket =>
        {
            var area = areaLookup.GetValueOrDefault(ticket.Id);
            return new DailySaleItem(
                ticket.Id,
                ticket.PurchasedAt,
                ticket.Event?.Title,
                ticket.SeatNumber,
                ticket.User?.FullName ?? ticket.User?.Email,
                area?.Price ?? 0
            );
        }).ToList();

        return Ok(new DailySalesResponse(
            tickets.Count,
            activity.Sum(item => item.Price),
            activity
        ));
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
                scan.ScannedCode ?? ticket?.QrCode,
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
                ScannedCode = qrCode,
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
                ScannedCode = qrCode,
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
            ScannedCode = qrCode,
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
        var userEmail = User.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? User.FindFirstValue(ClaimTypes.Email)
            ?? "unknown";
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

    private async Task<User> FindOrCreateCustomerAsync(string email, string? fullName, string? phone)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        if (user != null)
        {
            if (!string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(user.FullName))
            {
                user.FullName = fullName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(user.Phone))
            {
                user.Phone = phone.Trim();
            }

            return user;
        }

        user = new User
        {
            Email = normalizedEmail,
            FullName = string.IsNullOrWhiteSpace(fullName) ? normalizedEmail : fullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
            CreatedAt = DbTimeNow()
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task<Dictionary<Guid, EventArea>> BuildAreaLookupAsync(IEnumerable<Guid> ticketIds)
    {
        var ids = ticketIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, EventArea>();
        }

        return await _db.AreaSeats
            .AsNoTracking()
            .Include(seat => seat.EventArea)
            .Where(seat => seat.TicketId.HasValue && ids.Contains(seat.TicketId.Value))
            .GroupBy(seat => seat.TicketId!.Value)
            .Select(group => new { TicketId = group.Key, Area = group.First().EventArea })
            .ToDictionaryAsync(item => item.TicketId, item => item.Area);
    }

    private static bool IsSeatAvailable(string? status)
    {
        return string.Equals(status, "available", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "reserved", StringComparison.OrdinalIgnoreCase);
    }

    private static TicketItemResponse ToTicketItemResponse(Ticket ticket)
    {
        return new TicketItemResponse(ticket.Id, ticket.QrCode, ticket.SeatNumber, ticket.Status);
    }

    private static TicketPrintResponse ToTicketPrintResponse(Ticket ticket)
    {
        return new TicketPrintResponse(
            ticket.Id,
            ticket.QrCode,
            ticket.SeatNumber,
            ticket.Status,
            ticket.PurchasedAt,
            ticket.Event?.Id,
            ticket.Event?.Title,
            ticket.Event?.EventDate,
            ticket.User?.FullName ?? ticket.User?.Email,
            ticket.User?.Email
        );
    }

}

public class PurchasePosRequest
{
    public string Email { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public string? Phone { get; init; }
    public Guid EventId { get; init; }
    public long AreaId { get; init; }
    public List<string> Seats { get; init; } = [];
    public Guid? SellerId { get; init; }
}

public record TicketItemResponse(Guid Id, string QrCode, string? SeatNumber, string? Status);

public record PurchasePosResponse(List<TicketItemResponse> Tickets, int TotalTickets, decimal Total, string Message);

public record TicketPrintResponse(
    Guid Id,
    string QrCode,
    string? SeatNumber,
    string? Status,
    DateTime? PurchasedAt,
    Guid? EventId,
    string? EventTitle,
    DateTime? EventDate,
    string? HolderName,
    string? HolderEmail
);

public record PurchaseZoneResponse(long? Id, string? Name, decimal Price);

public record PurchaseHistoryResponse(
    Guid Id,
    Event? Event,
    PurchaseZoneResponse? Zone,
    List<TicketItemResponse> Tickets,
    decimal Total,
    string Status,
    DateTime? PurchasedAt
);

public record DailySalesResponse(int TotalTickets, decimal TotalRevenue, List<DailySaleItem> Activity);

public record DailySaleItem(Guid Id, DateTime? Time, string? EventTitle, string? Seat, string? Buyer, decimal Price);

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
