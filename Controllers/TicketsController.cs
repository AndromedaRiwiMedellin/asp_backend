using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using asp_backend.Data;
using asp_backend.models;

namespace asp_backend.Controllers;

[ApiController]
[Route("tickets")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TicketsController(AppDbContext db)
    {
        _db = db;
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

        return Ok(new { message = "Purchase successful", tickets = purchasedTickets });
    }
}

public class PosPurchaseRequest
{
    public string Email { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public long AreaId { get; set; }
    public List<string> Seats { get; set; } = new();
}
