namespace FamilyBudget.Domain.Entities;

public class UserPreference
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string PreferredTheme { get; set; } = "light"; // "light" or "dark"
    public string PreferredCurrency { get; set; } = "INR";
    public string? DefaultFamilyId { get; set; }
    public bool EmailNotificationsEnabled { get; set; } = true;

    // Navigation property
    public virtual ApplicationUser? User { get; set; }
}
