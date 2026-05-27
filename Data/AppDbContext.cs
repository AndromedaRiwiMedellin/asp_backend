using System;
using System.Collections.Generic;
using asp_backend.models;
using Microsoft.EntityFrameworkCore;

namespace asp_backend.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AreaSeat> AreaSeats { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<EventArea> EventAreas { get; set; }

    public virtual DbSet<EventSection> EventSections { get; set; }

    public virtual DbSet<Metric> Metrics { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Pqr> Pqrs { get; set; }

    public virtual DbSet<PqrsResponse> PqrsResponses { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<TicketScan> TicketScans { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<AreaSeat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("area_seats_pkey");

            entity.ToTable("area_seats");

            entity.HasIndex(e => e.EventAreaId, "idx_area_seats_area");

            entity.HasIndex(e => e.Status, "idx_area_seats_status");

            entity.HasIndex(e => e.UserId, "idx_area_seats_user");

            entity.HasIndex(e => new { e.EventAreaId, e.SeatNumber }, "uq_area_seat_number").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.EventAreaId).HasColumnName("event_area_id");
            entity.Property(e => e.ReservedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("reserved_at");
            entity.Property(e => e.RowLabel)
                .HasMaxLength(10)
                .HasColumnName("row_label");
            entity.Property(e => e.SeatNumber)
                .HasMaxLength(20)
                .HasColumnName("seat_number");
            entity.Property(e => e.SoldAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("sold_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'available'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.EventArea).WithMany(p => p.AreaSeats)
                .HasForeignKey(d => d.EventAreaId)
                .HasConstraintName("fk_area_seats_area");

            entity.HasOne(d => d.Ticket).WithMany(p => p.AreaSeats)
                .HasForeignKey(d => d.TicketId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_area_seats_ticket");

            entity.HasOne(d => d.User).WithMany(p => p.AreaSeats)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_area_seats_user");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("employees_pkey");

            entity.ToTable("employees");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Role).WithMany(p => p.Employees)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("employees_role_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Employees)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("employees_user_id_fkey");

            entity.HasMany(d => d.Permissions).WithMany(p => p.Employees)
                .UsingEntity<Dictionary<string, object>>(
                    "EmployeePermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("employee_permissions_permission_id_fkey"),
                    l => l.HasOne<Employee>().WithMany()
                        .HasForeignKey("EmployeeId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("employee_permissions_employee_id_fkey"),
                    j =>
                    {
                        j.HasKey("EmployeeId", "PermissionId").HasName("employee_permissions_pkey");
                        j.ToTable("employee_permissions");
                        j.IndexerProperty<Guid>("EmployeeId").HasColumnName("employee_id");
                        j.IndexerProperty<int>("PermissionId").HasColumnName("permission_id");
                    });
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("events_pkey");

            entity.ToTable("events");

            entity.HasIndex(e => e.EventDate, "idx_events_date");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EventDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("event_date");
            entity.Property(e => e.PosterUrl).HasColumnName("poster_url");
            entity.Property(e => e.SaleEnd)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("sale_end");
            entity.Property(e => e.SaleStart)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("sale_start");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.TotalCapacity).HasColumnName("total_capacity");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Events)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("events_created_by_fkey");
        });

        modelBuilder.Entity<EventArea>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("event_area_pkey");

            entity.ToTable("event_area");

            entity.HasIndex(e => new { e.EventId, e.AreaName }, "uq_event_area_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AreaName)
                .HasMaxLength(100)
                .HasColumnName("area_name");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Event).WithMany(p => p.EventAreas)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("fk_event_area_event");
        });

        modelBuilder.Entity<EventSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("event_sections_pkey");

            entity.ToTable("event_sections");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.SectionName)
                .HasMaxLength(100)
                .HasColumnName("section_name");

            entity.HasOne(d => d.Event).WithMany(p => p.EventSections)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("event_sections_event_id_fkey");
        });

        modelBuilder.Entity<Metric>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("metrics_pkey");

            entity.ToTable("metrics");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.MetricName)
                .HasMaxLength(100)
                .HasColumnName("metric_name");
            entity.Property(e => e.MetricValue).HasColumnName("metric_value");
            entity.Property(e => e.RecordedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("recorded_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Metrics)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("metrics_created_by_fkey");

            entity.HasOne(d => d.Event).WithMany(p => p.Metrics)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("metrics_event_id_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.Read)
                .HasDefaultValue(false)
                .HasColumnName("read");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("permissions_pkey");

            entity.ToTable("permissions");

            entity.HasIndex(e => e.Name, "permissions_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Pqr>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pqrs_pkey");

            entity.ToTable("pqrs");

            entity.HasIndex(e => e.Status, "idx_pqrs_status");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'OPEN'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Subject)
                .HasMaxLength(255)
                .HasColumnName("subject");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Pqrs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("pqrs_user_id_fkey");
        });

        modelBuilder.Entity<PqrsResponse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pqrs_responses_pkey");

            entity.ToTable("pqrs_responses");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.PqrsId).HasColumnName("pqrs_id");
            entity.Property(e => e.Response).HasColumnName("response");

            entity.HasOne(d => d.Employee).WithMany(p => p.PqrsResponses)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("pqrs_responses_employee_id_fkey");

            entity.HasOne(d => d.Pqrs).WithMany(p => p.PqrsResponses)
                .HasForeignKey(d => d.PqrsId)
                .HasConstraintName("pqrs_responses_pqrs_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.Name, "roles_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sessions_pkey");

            entity.ToTable("sessions");

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .HasColumnName("id");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasColumnName("ip_address");
            entity.Property(e => e.LastActivity).HasColumnName("last_activity");
            entity.Property(e => e.Payload).HasColumnName("payload");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.UserId)
                .HasMaxLength(255)
                .HasColumnName("user_id");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tickets_pkey");

            entity.ToTable("tickets");

            entity.HasIndex(e => e.QrCode, "idx_tickets_qr");

            entity.HasIndex(e => e.QrCode, "tickets_qr_code_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.PurchasedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("purchased_at");
            entity.Property(e => e.QrCode).HasColumnName("qr_code");
            entity.Property(e => e.SeatNumber)
                .HasMaxLength(50)
                .HasColumnName("seat_number");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'VALID'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Event).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("tickets_event_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("tickets_user_id_fkey");
        });

        modelBuilder.Entity<TicketScan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ticket_scans_pkey");

            entity.ToTable("ticket_scans");

            entity.HasIndex(e => e.TicketId, "idx_ticket_scans_ticket");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.ScannedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("scanned_at");
            entity.Property(e => e.ScannedBy).HasColumnName("scanned_by");
            entity.Property(e => e.Success)
                .HasDefaultValue(true)
                .HasColumnName("success");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");

            entity.HasOne(d => d.ScannedByNavigation).WithMany(p => p.TicketScans)
                .HasForeignKey(d => d.ScannedBy)
                .HasConstraintName("ticket_scans_scanned_by_fkey");

            entity.HasOne(d => d.Ticket).WithMany(p => p.TicketScans)
                .HasForeignKey(d => d.TicketId)
                .HasConstraintName("ticket_scans_ticket_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "idx_users_email");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.GoogleId).HasColumnName("google_id");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .HasColumnName("phone");
            entity.Property(e => e.ProfileImage).HasColumnName("profile_image");

            entity.HasMany(d => d.Events).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "Favorite",
                    r => r.HasOne<Event>().WithMany()
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("favorites_event_id_fkey"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("favorites_user_id_fkey"),
                    j =>
                    {
                        j.HasKey("UserId", "EventId").HasName("favorites_pkey");
                        j.ToTable("favorites");
                        j.IndexerProperty<Guid>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<Guid>("EventId").HasColumnName("event_id");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
