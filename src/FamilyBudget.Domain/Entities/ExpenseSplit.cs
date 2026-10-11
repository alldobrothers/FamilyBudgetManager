using FamilyBudget.Domain.Enums;

namespace FamilyBudget.Domain.Entities;

public class ExpenseSplit
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ExpenseTransactionId { get; set; } = string.Empty;
    public string BeneficiaryMemberId { get; set; } = string.Empty;
    public SplitType SplitType { get; set; } = SplitType.Equal;
    public decimal SplitAmount { get; set; }
    public decimal? SplitPercentage { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual ExpenseTransaction? ExpenseTransaction { get; set; }
    public virtual FamilyMembership? BeneficiaryMember { get; set; }
}
