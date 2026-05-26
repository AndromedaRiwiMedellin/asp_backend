using System;
using System.Collections.Generic;

namespace asp_backend.models;

public partial class AreaSeat
{
    public long Id { get; set; }

    public long EventAreaId { get; set; }

    public Guid? UserId { get; set; }

    public Guid? TicketId { get; set; }

    public string SeatNumber { get; set; } = null!;

    public string? RowLabel { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? ReservedAt { get; set; }

    public DateTime? SoldAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual EventArea EventArea { get; set; } = null!;

    public virtual Ticket? Ticket { get; set; }

    public virtual User? User { get; set; }
}
