using System;
using System.Collections.Generic;
using asp_backend.models;

namespace asp_backend.models;

public partial class PqrsResponse
{
    public Guid Id { get; set; }

    public Guid? PqrsId { get; set; }

    public Guid? EmployeeId { get; set; }

    public string? Response { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual Pqr? Pqrs { get; set; }
}
