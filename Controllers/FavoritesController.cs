using asp_backend.Data;
using asp_backend.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace asp_backend.Controllers;

[ApiController]
[Route("favorites")]
[Produces("application/json")]
[Tags("Favorites")]
[AllowAnonymous]
public class FavoritesController : ControllerBase
{
    private readonly AppDbContext _db;

    public FavoritesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<Event>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFavorites([FromQuery] Guid? userId = null, [FromQuery] string? email = null)
    {
        var user = await FindUserWithFavoritesAsync(userId, email);
        return Ok(user?.Events.ToList() ?? []);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFavorite([FromBody] FavoriteRequest request)
    {
        if (request == null || request.EventId == Guid.Empty)
        {
            return BadRequest(new { message = "eventId is required." });
        }

        var user = await FindUserWithFavoritesAsync(request.UserId, request.Email);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var ev = await _db.Events.FindAsync(request.EventId);
        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (!user.Events.Any(item => item.Id == ev.Id))
        {
            user.Events.Add(ev);
            await _db.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpDelete("{eventId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveFavorite(Guid eventId, [FromQuery] Guid? userId = null, [FromQuery] string? email = null)
    {
        var user = await FindUserWithFavoritesAsync(userId, email);
        if (user == null)
        {
            return NoContent();
        }

        var ev = user.Events.FirstOrDefault(item => item.Id == eventId);
        if (ev != null)
        {
            user.Events.Remove(ev);
            await _db.SaveChangesAsync();
        }

        return NoContent();
    }

    private Task<User?> FindUserWithFavoritesAsync(Guid? userId, string? email)
    {
        var query = _db.Users.Include(user => user.Events).AsQueryable();

        if (userId.HasValue)
        {
            return query.FirstOrDefaultAsync(user => user.Id == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return query.FirstOrDefaultAsync(user => user.Email.ToLower() == normalizedEmail);
        }

        return Task.FromResult<User?>(null);
    }
}

public class FavoriteRequest
{
    public Guid? UserId { get; init; }
    public string? Email { get; init; }
    public Guid EventId { get; init; }
}
