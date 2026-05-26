using System;
using System.Collections.Generic;

namespace asp_backend.models;

public partial class Pqr
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string? Type { get; set; }

    public string? Subject { get; set; }

    public string? Message { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<PqrsResponse> PqrsResponses { get; set; } = new List<PqrsResponse>();

    public virtual User? User { get; set; }
}
