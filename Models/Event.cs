namespace asp_backend.Models;

/// <summary>
/// Represents an event published in the platform.
/// </summary>
public class Event
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Human-readable title of the event.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the event.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Public poster image URL for the event.
    /// </summary>
    public string? PosterUrl { get; set; }

    /// <summary>
    /// Scheduled date and time of the event.
    /// </summary>
    public DateTime EventDate { get; set; }

    /// <summary>
    /// Date and time when ticket sales begin.
    /// </summary>
    public DateTime SaleStart { get; set; }

    /// <summary>
    /// Date and time when ticket sales end.
    /// </summary>
    public DateTime SaleEnd { get; set; }

    /// <summary>
    /// Total capacity for the event.
    /// </summary>
    public int TotalCapacity { get; set; }

    /// <summary>
    /// Identifier of the user who created the event.
    /// </summary>
    public int CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the event record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}