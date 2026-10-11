using Microsoft.AspNetCore.Identity;

namespace FamilyBudget.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation collections
    public virtual ICollection<FamilyMembership> Memberships { get; set; } = new List<FamilyMembership>();
    public virtual ICollection<InAppNotification> Notifications { get; set; } = new List<InAppNotification>();
    public virtual UserPreference? Preference { get; set; }
}
