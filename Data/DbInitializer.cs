using asp_backend.models;
using Microsoft.EntityFrameworkCore;

namespace asp_backend.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var hadUsers = await db.Users.AnyAsync();

        await EnsureOperationalAdminAsync(db, now);

        if (hadUsers)
        {
            return;
        }

        var employeeRole = await db.Roles.FirstAsync(role => role.Name == "employee");

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

        var ticketsPermission = await db.Permissions.FirstAsync(permission => permission.Name == "tickets");
        var securityPermission = await db.Permissions.FirstAsync(permission => permission.Name == "seguridad");

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

    private static async Task EnsureOperationalAdminAsync(AppDbContext db, DateTime now)
    {
        var adminRole = await EnsureRoleAsync(db, "admin");
        await EnsureRoleAsync(db, "employee");
        await EnsureRoleAsync(db, "superadmin");

        var ticketsPermission = await EnsurePermissionAsync(db, "tickets");
        var securityPermission = await EnsurePermissionAsync(db, "seguridad");

        var adminUser = await db.Users
            .FirstOrDefaultAsync(user => user.Email.ToLower() == "admin@boletas.com");

        if (adminUser == null)
        {
            adminUser = new User
            {
                Email = "admin@boletas.com",
                FullName = "Administrador OrbiX",
                CreatedAt = now
            };
            db.Users.Add(adminUser);
        }

        adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123");
        adminUser.FullName = string.IsNullOrWhiteSpace(adminUser.FullName)
            ? "Administrador OrbiX"
            : adminUser.FullName;

        await db.SaveChangesAsync();

        var adminEmployee = await db.Employees
            .Include(employee => employee.Permissions)
            .FirstOrDefaultAsync(employee => employee.UserId == adminUser.Id);

        if (adminEmployee == null)
        {
            adminEmployee = new Employee
            {
                UserId = adminUser.Id,
                CreatedAt = now,
                Permissions = new List<Permission>()
            };
            db.Employees.Add(adminEmployee);
        }

        adminEmployee.RoleId = adminRole.Id;
        adminEmployee.Active = true;

        if (!adminEmployee.Permissions.Any(permission => permission.Id == ticketsPermission.Id))
        {
            adminEmployee.Permissions.Add(ticketsPermission);
        }

        if (!adminEmployee.Permissions.Any(permission => permission.Id == securityPermission.Id))
        {
            adminEmployee.Permissions.Add(securityPermission);
        }

        await db.SaveChangesAsync();
    }

    private static async Task<Role> EnsureRoleAsync(AppDbContext db, string name)
    {
        var role = await db.Roles.FirstOrDefaultAsync(item => item.Name == name);
        if (role != null)
        {
            return role;
        }

        role = new Role { Name = name };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role;
    }

    private static async Task<Permission> EnsurePermissionAsync(AppDbContext db, string name)
    {
        var permission = await db.Permissions.FirstOrDefaultAsync(item => item.Name == name);
        if (permission != null)
        {
            return permission;
        }

        permission = new Permission { Name = name };
        db.Permissions.Add(permission);
        await db.SaveChangesAsync();
        return permission;
    }
}
