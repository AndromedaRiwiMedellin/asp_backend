using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using asp_backend.Data;

namespace asp_backend.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var events = await _db.Events.ToListAsync();
        return Ok(events);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev == null)
            return NotFound(new { message = "Evento no encontrado" });

        var areas = await _db.EventAreas
            .Where(a => a.EventId == id)
            .ToListAsync();

        return Ok(new { ev, areas });
    }

    [HttpPost("{id}/seats/lock")]
    public async Task<IActionResult> LockSeats(int id, [FromBody] LockSeatsRequest request)
    {
        var seats = await _db.AreaSeats
            .Where(s => request.Seats.Contains(s.SeatNumber) && s.EventAreaId == id)
            .ToListAsync();

        if (seats.Count != request.Seats.Count)
            return BadRequest(new { message = "Uno o más asientos no existen" });

        var unavailable = seats.Where(s => s.Status != "available").ToList();
        if (unavailable.Any())
            return Conflict(new { message = "Uno o más asientos no están disponibles" });

        foreach (var seat in seats)
        {
            seat.Status = "reserved";
            seat.ReservedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Asientos bloqueados", seats = request.Seats });
    }
}

public record LockSeatsRequest(List<string> Seats);