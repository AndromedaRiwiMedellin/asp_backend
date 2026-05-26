using System;
using System.Collections.Generic;

namespace asp_backend.models;

public partial class Ticket
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public Guid? EventId { get; set; }

    public string QrCode { get; set; } = null!;

    public string? SeatNumber { get; set; }

    public string? Status { get; set; }

    public DateTime? PurchasedAt { get; set; }

    public virtual ICollection<AreaSeat> AreaSeats { get; set; } = new List<AreaSeat>();

    public virtual Event? Event { get; set; }

    public virtual ICollection<TicketScan> TicketScans { get; set; } = new List<TicketScan>();

    public virtual User? User { get; set; }
}
