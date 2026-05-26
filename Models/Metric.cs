using System;
using System.Collections.Generic;

namespace asp_backend.Models;

public partial class Metric
{
    public Guid Id { get; set; }

    public string? MetricName { get; set; }

    public decimal? MetricValue { get; set; }

    public DateTime? RecordedAt { get; set; }

    public Guid? EventId { get; set; }

    public Guid? CreatedBy { get; set; }

    public virtual Employee? CreatedByNavigation { get; set; }

    public virtual Event? Event { get; set; }
}
