using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using asp_backend.Data;
using asp_backend.Models;

namespace asp_backend.Controllers;

/// <summary>
/// Endpoints related to event discovery and seat management.
/// </summary>
[ApiController]
[Route("events")]
[Produces("application/json")]
[Tags("Events")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns all events available in the system.
    /// </summary>
    /// <remarks>
    /// Useful for displaying the event catalog in the client application.
    /// </remarks>
    /// <response code="200">A collection of events.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<Event>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var events = await _db.Events.ToListAsync();
        return Ok(events);
    }

    /// <summary>
    /// Retrieves a single event with its areas.
    /// </summary>
    /// <param name="id">Identifier of the event to fetch.</param>
    /// <response code="200">The event and its associated areas.</response>
    /// <response code="404">The provided event identifier does not exist.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var ev = await _db.Events
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev == null)
        {
            return NotFound(new { message = "Event not found" });
        }

        var areas = await _db.EventAreas
            .Where(a => a.EventId == id)
            .ToListAsync();

        return Ok(new EventDetailsResponse(ev, areas));
    }

    /// <summary>
    /// Reserves the provided seats for an event area.
    /// </summary>
    /// <param name="id">Identifier of the event area.</param>
    /// <param name="request">Seats to reserve.</param>
    /// <response code="200">The requested seats were successfully reserved.</response>
    /// <response code="400">One or more seats are invalid for the supplied event area.</response>
    /// <response code="409">One or more seats are already unavailable.</response>
    [HttpPost("{id}/seats/lock")]
    [ProducesResponseType(typeof(LockSeatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LockSeats(int id, [FromBody] LockSeatsRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        var seats = await _db.AreaSeats
            .Where(s => request.Seats.Contains(s.SeatNumber) && s.EventAreaId == id)
            .ToListAsync();

        if (seats.Count != request.Seats.Count)
        {
            return BadRequest(new { message = "One or more seats do not exist." });
        }

        var unavailable = seats.Where(s => s.Status != "available").ToList();
        if (unavailable.Any())
        {
            return Conflict(new { message = "One or more seats are not available" });
        }

        foreach (var seat in seats)
        {
            seat.Status = "reserved";
            seat.ReservedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new LockSeatsResponse("Reserved seats", request.Seats));
    }
}

/// <summary>
/// Request payload used to lock seats in an event area.
/// </summary>
public class LockSeatsRequest
{
    /// <summary>
    /// Seat numbers to reserve. The values must exist in the selected event area.
    /// </summary>
    public List<string> Seats { get; init; } = [];
}

/// <summary>
/// Event payload returned when fetching one event.
/// </summary>
public record EventDetailsResponse(Event Event, List<EventArea> Areas);

/// <summary>
/// Response returned after successfully reserving seats.
/// </summary>
public record LockSeatsResponse(string Message, List<string> Seats);