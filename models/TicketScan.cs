using System;
using System.Collections.Generic;

namespace asp_backend.models;

public partial class TicketScan
{
    public Guid Id { get; set; }

    public Guid? TicketId { get; set; }

    public Guid? ScannedBy { get; set; }

    public DateTime? ScannedAt { get; set; }

    public bool? Success { get; set; }

    public string? Reason { get; set; }

    public virtual Employee? ScannedByNavigation { get; set; }

    public virtual Ticket? Ticket { get; set; }
}
