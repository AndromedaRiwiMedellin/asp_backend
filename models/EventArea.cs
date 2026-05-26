using System;
using System.Collections.Generic;

namespace asp_backend.models;

public partial class EventArea
{
    public long Id { get; set; }

    public Guid EventId { get; set; }

    public string AreaName { get; set; } = null!;

    public decimal Price { get; set; }

    public int Capacity { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AreaSeat> AreaSeats { get; set; } = new List<AreaSeat>();

    public virtual Event Event { get; set; } = null!;
}
