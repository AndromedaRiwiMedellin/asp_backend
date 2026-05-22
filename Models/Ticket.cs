namespace asp_backend.Models;

public class Ticket
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int EventId { get; set; }
    public string QrCode { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime PurchasedAt { get; set; }
}