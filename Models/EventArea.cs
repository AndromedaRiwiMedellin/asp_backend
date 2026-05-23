namespace asp_backend.Models;

/// <summary>
/// Represents a ticketing area within an event.
/// </summary>
public class EventArea
{
    /// <summary>
    /// Unique identifier for the event area.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Identifier of the parent event.
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// Name of the area, such as VIP or General.
    /// </summary>
    public string AreaName { get; set; } = string.Empty;

    /// <summary>
    /// Price configured for the area.
    /// </summary>
    public int Price { get; set; }

    /// <summary>
    /// Capacity available in the area.
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Optional description for the area.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Date and time when the area record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date and time when the area record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}