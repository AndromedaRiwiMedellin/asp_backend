using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using asp_backend.Data;
using asp_backend.models;
using System.Diagnostics;
using System.Text;

namespace asp_backend.Controllers;

[ApiController]
[Route("tickets")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly string _printerName;

    public TicketsController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _printerName = config["Printer:Name"] ?? string.Empty;
    }

    [HttpGet("daily-sales")]
    public async Task<IActionResult> GetDailySales([FromQuery] Guid sellerId)
    {
        var today = DateTime.Today;
        
        var salesData = await _db.Tickets
            .Include(t => t.Event)
            .Include(t => t.User)
            .Include(t => t.AreaSeats)
                .ThenInclude(s => s.EventArea)
            .Where(t => t.SellerId == sellerId && t.PurchasedAt >= today && t.PurchasedAt < today.AddDays(1))
            .ToListAsync();

        var totalTickets = salesData.Count;
        var totalRevenue = salesData.Sum(t => t.AreaSeats.FirstOrDefault()?.EventArea?.Price ?? 0);

        return Ok(new {
            totalTickets,
            totalRevenue,
            activity = salesData.Select(t => new {
                id = t.Id,
                time = t.PurchasedAt,
                seat = t.SeatNumber,
                price = t.AreaSeats.FirstOrDefault()?.EventArea?.Price,
                eventTitle = t.Event?.Title,
                buyer = t.User?.FullName ?? t.User?.Email
            }).OrderByDescending(t => t.time)
        });
    }

    [HttpPost("purchase-pos")]
    public async Task<IActionResult> PurchasePos([FromBody] PosPurchaseRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || request.Seats == null || !request.Seats.Any())
        {
            return BadRequest(new { message = "Invalid request." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var requestedSeats = request.Seats
            .Where(seat => !string.IsNullOrWhiteSpace(seat))
            .Select(seat => seat.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requestedSeats.Count == 0)
        {
            return BadRequest(new { message = "At least one seat is required." });
        }

        var eventExists = await _db.Events.AnyAsync(e => e.Id == request.EventId);
        if (!eventExists)
        {
            return NotFound(new { message = "Event not found." });
        }

        var areaExists = await _db.EventAreas.AnyAsync(a => a.Id == request.AreaId && a.EventId == request.EventId);
        if (!areaExists)
        {
            return BadRequest(new { message = "Area does not belong to the selected event." });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                FullName = string.IsNullOrWhiteSpace(request.FullName) ? normalizedEmail : request.FullName.Trim(),
                Phone = request.Phone,
                CreatedAt = DbNow()
            };
            _db.Users.Add(user);
        }

        var seats = await _db.AreaSeats
            .Where(s => requestedSeats.Contains(s.SeatNumber) && s.EventAreaId == request.AreaId)
            .ToListAsync();

        if (seats.Count != requestedSeats.Count)
        {
            return BadRequest(new { message = "Some seats do not exist." });
        }

        if (seats.Any(s => !string.Equals(s.Status, "available", StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new { message = "Some seats are already unavailable." });
        }

        var purchasedTickets = new List<Ticket>();
        var purchasedAt = DbNow();

        foreach (var seat in seats)
        {
            var ticket = new Ticket
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                SellerId = request.SellerId,
                EventId = request.EventId,
                QrCode = Guid.NewGuid().ToString(),
                SeatNumber = seat.SeatNumber,
                Status = "valid",
                PurchasedAt = purchasedAt
            };

            _db.Tickets.Add(ticket);
            purchasedTickets.Add(ticket);

            seat.Status = "sold";
            seat.UserId = user.Id;
            seat.TicketId = ticket.Id;
            seat.SoldAt = purchasedAt;
            seat.UpdatedAt = purchasedAt;
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new PosPurchaseResponse(
            "Purchase successful",
            purchasedTickets.Select(ticket => new PurchasedTicketResponse(
                ticket.Id,
                ticket.QrCode,
                ticket.SeatNumber,
                ticket.Status,
                ticket.PurchasedAt
            )).ToList()
        ));
    }

    [HttpGet("purchases")]
    public async Task<IActionResult> GetPurchases([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "Email is required." });
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var tickets = await _db.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.User)
            .Include(ticket => ticket.Event)
            .Include(ticket => ticket.AreaSeats)
                .ThenInclude(seat => seat.EventArea)
            .Where(ticket => ticket.User != null && ticket.User.Email.ToLower() == normalizedEmail)
            .OrderByDescending(ticket => ticket.PurchasedAt)
            .ToListAsync();

        var purchases = tickets
            .GroupBy(ticket => new
            {
                ticket.EventId,
                PurchasedAt = ticket.PurchasedAt?.ToString("O") ?? string.Empty
            })
            .Select(group =>
            {
                var firstTicket = group.First();
                var eventArea = firstTicket.AreaSeats.FirstOrDefault()?.EventArea;
                var ticketItems = group
                    .Select(ticket => new PurchasedTicketResponse(
                        ticket.Id,
                        ticket.QrCode,
                        ticket.SeatNumber,
                        ticket.Status,
                        ticket.PurchasedAt
                    ))
                    .ToList();

                return new PurchaseHistoryResponse(
                    Id: $"{firstTicket.EventId}-{group.Key.PurchasedAt}",
                    Event: new PurchaseEventResponse(
                        firstTicket.Event?.Id,
                        firstTicket.Event?.Title ?? "Evento OrbiX",
                        firstTicket.Event?.EventDate,
                        firstTicket.Event?.PosterUrl
                    ),
                    Zone: new PurchaseZoneResponse(
                        eventArea?.Id,
                        eventArea?.AreaName ?? "Zona",
                        eventArea?.Price ?? 0
                    ),
                    Tickets: ticketItems,
                    Total: ticketItems.Count * (eventArea?.Price ?? 0),
                    Status: "Pagado",
                    PurchasedAt: firstTicket.PurchasedAt
                );
            })
            .ToList();

        return Ok(purchases);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var ticket = await _db.Tickets
            .Include(t => t.User)
            .Include(t => t.Event)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
            return NotFound(new { message = "Ticket not found." });

        var response = new TicketPrintResponse(
            Id:          ticket.Id.ToString(),
            QrCode:      ticket.QrCode,
            EventTitle:  ticket.Event?.Title ?? "—",
            EventDate:   ticket.Event?.EventDate,
            SeatNumber:  ticket.SeatNumber,
            HolderName:  ticket.User?.FullName ?? ticket.User?.Email ?? "—",
            PurchasedAt: ticket.PurchasedAt,
            Status:      ticket.Status ?? "valid"
        );

        return Ok(response);
    }

    [HttpPost("{id:guid}/print")]
    public async Task<IActionResult> PrintTicket(Guid id)
    {
        // Buscar ticket con sus relaciones
        var ticket = await _db.Tickets
            .Include(t => t.User)
            .Include(t => t.Event)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
            return NotFound(new { message = "Ticket not found." });

        if (string.IsNullOrEmpty(_printerName))
            return StatusCode(500, new { message = "Impresora no configurada en appsettings.json (Printer:Name)." });

        // ── Formatear como texto para impresora térmica 58mm ──
        var W = 32; // columnas a 58mm con font normal
        var sep = new string('-', W);
        var fmtDate = (DateTime? d) => d.HasValue
            ? d.Value.ToString("dd/MM/yyyy HH:mm")
            : "—";

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("ANDROMEDA".PadLeft((W + "ANDROMEDA".Length) / 2));
        sb.AppendLine(sep);
        sb.AppendLine((ticket.Event?.Title ?? "EVENTO").PadLeft((W + (ticket.Event?.Title?.Length ?? 6)) / 2));
        sb.AppendLine(sep);
        sb.AppendLine($"FECHA   : {fmtDate(ticket.Event?.EventDate)}");
        if (!string.IsNullOrEmpty(ticket.SeatNumber))
            sb.AppendLine($"ASIENTO : {ticket.SeatNumber}");
        sb.AppendLine($"TITULAR : {ticket.User?.FullName ?? ticket.User?.Email ?? "—"}");
        sb.AppendLine($"COMPRA  : {fmtDate(ticket.PurchasedAt)}");
        sb.AppendLine(sep);
        sb.AppendLine($"#{ticket.Id.ToString()[..8].ToUpper()}  [{ticket.Status?.ToUpper()}]");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine();  // avance de papel

        // ── Enviar a CUPS via lp ──
        var args = string.IsNullOrEmpty(_printerName)
            ? ""
            : $"-d \"{_printerName}\"";

        var psi = new ProcessStartInfo
        {
            FileName               = "lp",
            Arguments              = args,
            RedirectStandardInput  = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        try
        {
            using var process = Process.Start(psi)!;
            await process.StandardInput.WriteAsync(sb.ToString());
            process.StandardInput.Close();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var err = await process.StandardError.ReadToEndAsync();
                return StatusCode(500, new { message = $"Error al imprimir: {err.Trim()}" });
            }

            return Ok(new { message = "✓ Impresión enviada correctamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"No se pudo ejecutar lp: {ex.Message}" });
        }
    }

    private static DateTime DbNow() => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
}

public record TicketPrintResponse(
    string Id,
    string QrCode,
    string EventTitle,
    DateTime? EventDate,
    string? SeatNumber,
    string HolderName,
    DateTime? PurchasedAt,
    string Status
);

public class PosPurchaseRequest
{
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public Guid EventId { get; set; }
    public long AreaId { get; set; }
    public List<string> Seats { get; set; } = new();
    public Guid? SellerId { get; set; }
}

public record PosPurchaseResponse(string Message, List<PurchasedTicketResponse> Tickets);

public record PurchasedTicketResponse(
    Guid Id,
    string QrCode,
    string? SeatNumber,
    string? Status,
    DateTime? PurchasedAt
);

public record PurchaseHistoryResponse(
    string Id,
    PurchaseEventResponse Event,
    PurchaseZoneResponse Zone,
    List<PurchasedTicketResponse> Tickets,
    decimal Total,
    string Status,
    DateTime? PurchasedAt
);

public record PurchaseEventResponse(
    Guid? Id,
    string Title,
    DateTime? EventDate,
    string? Image
);

public record PurchaseZoneResponse(
    long? Id,
    string Name,
    decimal Price
);
