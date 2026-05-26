using System;
using System.Collections.Generic;

namespace asp_backend.Models;

public partial class Notification
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string? Title { get; set; }

    public string? Message { get; set; }

    public bool? Read { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
