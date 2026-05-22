using Microsoft.EntityFrameworkCore;
using asp_backend.Models;

namespace asp_backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventArea> EventAreas => Set<EventArea>();
    public DbSet<EventSection> EventSections => Set<EventSection>();
    public DbSet<AreaSeat> AreaSeats => Set<AreaSeat>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketScan> TicketScans => Set<TicketScan>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Role> Roles => Set<Role>();
}