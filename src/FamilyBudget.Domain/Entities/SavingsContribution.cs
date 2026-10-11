namespace FamilyBudget.Domain.Entities;

public class SavingsContribution
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SavingsGoalId { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ContributionDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual SavingsGoal? SavingsGoal { get; set; }
    public virtual FamilyMembership? Member { get; set; }
}
