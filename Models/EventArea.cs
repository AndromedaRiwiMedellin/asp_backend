namespace asp_backend.Models;

public class EventArea
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string AreaName { get; set; } = string.Empty;
    public int Price { get; set; }
    public int Capacity { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}