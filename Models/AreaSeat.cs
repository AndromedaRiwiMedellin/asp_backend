namespace asp_backend.Models;

public class AreaSeat
{
    public int Id { get; set; }
    public int EventAreaId { get; set; }
    public int? UserId { get; set; }
    public int? TicketId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string RowLabel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ReservedAt { get; set; }
    public DateTime? SoldAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}