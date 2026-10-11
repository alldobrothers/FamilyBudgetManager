namespace FamilyBudget.Domain.Entities;

public class FamilySettings
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FamilyId { get; set; } = string.Empty;
    public bool EnableExpenseApprovals { get; set; } = false;
    public decimal BudgetWarningThreshold1 { get; set; } = 75.0m; // 75%
    public decimal BudgetWarningThreshold2 { get; set; } = 90.0m; // 90%
    public bool NotifyOnOverdueBills { get; set; } = true;
    public bool DarkModeDefault { get; set; } = false;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual Family? Family { get; set; }
}
