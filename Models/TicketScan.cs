namespace asp_backend.Models;

public class TicketScan
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int ScannedBy { get; set; }
    public DateTime ScannedAt { get; set; }
    public bool Success { get; set; }
    public string? Reason { get; set; }
}