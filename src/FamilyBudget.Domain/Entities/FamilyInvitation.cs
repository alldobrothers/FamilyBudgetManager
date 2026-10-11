using FamilyBudget.Domain.Enums;

namespace FamilyBudget.Domain.Entities;

public class FamilyInvitation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FamilyId { get; set; } = string.Empty;
    public string InvitedEmail { get; set; } = string.Empty;
    public FamilyRole InvitedRole { get; set; } = FamilyRole.Member;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
    public string InvitedByUserId { get; set; } = string.Empty;
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public string? AcceptedByUserId { get; set; }
    public DateTime? AcceptedAt { get; set; }

    // Navigation properties
    public virtual Family? Family { get; set; }
    public virtual ApplicationUser? InvitedByUser { get; set; }
    public virtual ApplicationUser? AcceptedByUser { get; set; }
}
