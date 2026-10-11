namespace FamilyBudget.Domain.Entities;

public class AuditLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? FamilyId { get; set; }
    public string? UserId { get; set; }
    public string ActionType { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, REVERSE, APPROVE, INVITE
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Family? Family { get; set; }
    public virtual ApplicationUser? User { get; set; }
}
