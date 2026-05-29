using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using asp_backend.Data;
using asp_backend.models;
using System.Diagnostics;
using System.Text;
using asp_backend.Services;

namespace asp_backend.Controllers;

[ApiController]
[Route("tickets")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly string _printerName;
    private readonly IEmailService _emailService;

    public TicketsController(AppDbContext db, IConfiguration config, IEmailService emailService)
    {
        _db = db;
        _printerName = config["Printer:Name"] ?? string.Empty;
        _emailService = emailService;
    }

    [HttpGet("daily-sales")]
    public async Task<IActionResult> GetDailySales([FromQuery] Guid sellerId, [FromQuery] DateTime? date = null)
    {
        var targetDate = date?.Date ?? DateTime.Today;
        
        var salesData = await _db.Tickets
            .Include(t => t.Event)
            .Include(t => t.User)
            .Include(t => t.AreaSeats)
                .ThenInclude(s => s.EventArea)
            .Where(t => t.SellerId == sellerId && t.PurchasedAt >= targetDate && t.PurchasedAt < targetDate.AddDays(1))
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
        if (request == null || string.IsNullOrEmpty(request.Email) || request.Seats == null || !request.Seats.Any())
        {
            return BadRequest(new { message = "Invalid request." });
        }

        // Buscar o crear usuario
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FullName = request.FullName,
                Phone = request.Phone,
                CreatedAt = DateTime.Now
            };
            _db.Users.Add(user);
        }

        // Buscar asientos
        var seats = await _db.AreaSeats
            .Where(s => request.Seats.Contains(s.SeatNumber) && s.EventAreaId == request.AreaId)
            .ToListAsync();

        if (seats.Count != request.Seats.Count)
        {
            return BadRequest(new { message = "Some seats do not exist." });
        }

        if (seats.Any(s => s.Status == "sold"))
        {
            return Conflict(new { message = "Some seats are already sold." });
        }

        var purchasedTickets = new List<Ticket>();

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
                PurchasedAt = DateTime.Now
            };

            _db.Tickets.Add(ticket);
            purchasedTickets.Add(ticket);

            seat.Status = "sold";
            seat.UserId = user.Id;
            seat.TicketId = ticket.Id;
            seat.SoldAt = DateTime.Now;
            seat.UpdatedAt = DateTime.Now;
        }

        await _db.SaveChangesAsync();

        // Enviar correo a los clientes (de forma asíncrona pero sin bloquear la respuesta)
        var eventDetails = await _db.Events.FirstOrDefaultAsync(e => e.Id == request.EventId);
        var eventTitle = eventDetails?.Title ?? "Evento Orbix";
        var eventDate = eventDetails?.EventDate;

        foreach (var ticket in purchasedTickets)
        {
            _ = _emailService.SendTicketEmailAsync(
                user.Email, 
                user.FullName, 
                eventTitle, 
                eventDate, 
                ticket.SeatNumber, 
                ticket.QrCode, 
                ticket.Id.ToString()
            );
        }

        return Ok(new { message = "Purchase successful", tickets = purchasedTickets });
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
