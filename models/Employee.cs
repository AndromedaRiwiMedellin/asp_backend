using System;
using System.Collections.Generic;
using asp_backend.Models;

namespace asp_backend.models;

public partial class Employee
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public int? RoleId { get; set; }

    public bool? Active { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ICollection<Metric> Metrics { get; set; } = new List<Metric>();

    public virtual ICollection<PqrsResponse> PqrsResponses { get; set; } = new List<PqrsResponse>();

    public virtual Role? Role { get; set; }

    public virtual ICollection<TicketScan> TicketScans { get; set; } = new List<TicketScan>();

    public virtual User? User { get; set; }

    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
