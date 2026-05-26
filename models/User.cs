using System;
using System.Collections.Generic;

namespace asp_backend.models;

public partial class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string? GoogleId { get; set; }

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public string? ProfileImage { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<AreaSeat> AreaSeats { get; set; } = new List<AreaSeat>();

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Pqr> Pqrs { get; set; } = new List<Pqr>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}
