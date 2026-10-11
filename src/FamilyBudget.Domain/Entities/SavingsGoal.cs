using FamilyBudget.Domain.Enums;

namespace FamilyBudget.Domain.Entities;

public class SavingsGoal
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FamilyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; } = 0.0m;
    public DateTime? TargetDate { get; set; }
    public string? Category { get; set; }
    public string? Notes { get; set; }
    public SavingsGoalStatus Status { get; set; } = SavingsGoalStatus.Active;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Family? Family { get; set; }
    public virtual ApplicationUser? CreatedByUser { get; set; }
    public virtual ICollection<SavingsContribution> Contributions { get; set; } = new List<SavingsContribution>();
}
