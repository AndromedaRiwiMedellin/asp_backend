using asp_backend.models;
using Microsoft.EntityFrameworkCore;

namespace asp_backend.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        var employeeRole = new Role
        {
            Id = 1,
            Name = "employee"
        };

        db.Roles.Add(employeeRole);

        var employeeUser = new User
        {
            Email = "empleado@andromeda.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Empleado123!"),
            FullName = "Empleado Demo",
            CreatedAt = now
        };

        var attendeeOne = new User
        {
            Email = "cliente1@andromeda.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cliente123!"),
            FullName = "Carlos Gomez",
            CreatedAt = now
        };

        var attendeeTwo = new User
        {
            Email = "cliente2@andromeda.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Cliente123!"),
            FullName = "Ana Ruiz",
            CreatedAt = now
        };

        db.Users.AddRange(employeeUser, attendeeOne, attendeeTwo);
        await db.SaveChangesAsync();

        var ticketsPermission = new Permission { Name = "tickets" };
        var securityPermission = new Permission { Name = "seguridad" };
        db.Permissions.AddRange(ticketsPermission, securityPermission);
        await db.SaveChangesAsync();

        var employee = new Employee
        {
            UserId = employeeUser.Id,
            RoleId = employeeRole.Id,
            Active = true,
            CreatedAt = now,
            Permissions = new List<Permission> { ticketsPermission, securityPermission }
        };

        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        var ev = new Event
        {
            Title = "Concierto Andromeda 2026",
            Description = "Evento de prueba para escaneo de tickets.",
            EventDate = now.AddDays(7),
            SaleStart = now.AddDays(-15),
            SaleEnd = now.AddDays(6),
            TotalCapacity = 500,
            CreatedBy = employee.Id,
            CreatedAt = now
        };

        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var area = new EventArea
        {
            EventId = ev.Id,
            AreaName = "Platea A",
            Price = 120000,
            Capacity = 200,
            Description = "Zona principal",
            CreatedAt = now,
            UpdatedAt = now
        };

        db.EventAreas.Add(area);
        await db.SaveChangesAsync();

        var validTicket = new Ticket
        {
            UserId = attendeeOne.Id,
            EventId = ev.Id,
            QrCode = "ANDRO-VALID-001",
            SeatNumber = "A-15",
            Status = "VALID",
            PurchasedAt = now.AddDays(-2)
        };

        var fraudTicket = new Ticket
        {
            UserId = attendeeTwo.Id,
            EventId = ev.Id,
            QrCode = "ANDRO-FRAUD-001",
            SeatNumber = "B-22",
            Status = "FRAUD",
            PurchasedAt = now.AddDays(-1)
        };

        var illegalTicket = new Ticket
        {
            UserId = attendeeTwo.Id,
            EventId = ev.Id,
            QrCode = "ANDRO-ILLEGAL-001",
            SeatNumber = "C-09",
            Status = "ILLEGAL",
            PurchasedAt = now.AddHours(-6)
        };

        db.Tickets.AddRange(validTicket, fraudTicket, illegalTicket);
        await db.SaveChangesAsync();

        db.AreaSeats.AddRange(
            new AreaSeat
            {
                EventAreaId = area.Id,
                UserId = attendeeOne.Id,
                TicketId = validTicket.Id,
                SeatNumber = "A-15",
                RowLabel = "A",
                Status = "sold",
                SoldAt = now.AddDays(-2),
                CreatedAt = now,
                UpdatedAt = now
            },
            new AreaSeat
            {
                EventAreaId = area.Id,
                UserId = attendeeTwo.Id,
                TicketId = fraudTicket.Id,
                SeatNumber = "B-22",
                RowLabel = "B",
                Status = "sold",
                SoldAt = now.AddDays(-1),
                CreatedAt = now,
                UpdatedAt = now
            },
            new AreaSeat
            {
                EventAreaId = area.Id,
                UserId = attendeeTwo.Id,
                TicketId = illegalTicket.Id,
                SeatNumber = "C-09",
                RowLabel = "C",
                Status = "sold",
                SoldAt = now.AddHours(-6),
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        await db.SaveChangesAsync();
    }
}
