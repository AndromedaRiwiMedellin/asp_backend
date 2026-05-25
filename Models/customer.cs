namespace asp_backend.Models;

public class Customer
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public bool IsEmailVerified { get; set; } = false;
    public string? VerificationToken { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}