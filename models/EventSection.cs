using System;
using System.Collections.Generic;

namespace asp_backend.models;

public partial class EventSection
{
    public Guid Id { get; set; }

    public Guid? EventId { get; set; }

    public string? SectionName { get; set; }

    public decimal? Price { get; set; }

    public int? Capacity { get; set; }

    public virtual Event? Event { get; set; }
}
