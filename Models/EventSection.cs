namespace asp_backend.Models;

public class EventSection
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public int Price { get; set; }
    public int Capacity { get; set; }
}