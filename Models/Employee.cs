namespace asp_backend.Models;

public class Employee
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
}