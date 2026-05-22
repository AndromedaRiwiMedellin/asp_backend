namespace asp_backend.Models;

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public DateTime EventDate { get; set; }
    public DateTime SaleStart { get; set; }
    public DateTime SaleEnd { get; set; }
    public int TotalCapacity { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}