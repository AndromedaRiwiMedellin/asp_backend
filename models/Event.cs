using System;
using System.Collections.Generic;
using asp_backend.Models;

namespace asp_backend.models;

public partial class Event
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? PosterUrl { get; set; }

    public DateTime? EventDate { get; set; }

    public DateTime? SaleStart { get; set; }

    public DateTime? SaleEnd { get; set; }

    public int? TotalCapacity { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Employee? CreatedByNavigation { get; set; }

    public virtual ICollection<EventArea> EventAreas { get; set; } = new List<EventArea>();

    public virtual ICollection<EventSection> EventSections { get; set; } = new List<EventSection>();

    public virtual ICollection<Metric> Metrics { get; set; } = new List<Metric>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
