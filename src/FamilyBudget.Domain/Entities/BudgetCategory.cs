namespace FamilyBudget.Domain.Entities;

public class BudgetCategory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string BudgetId { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public decimal PlannedAmount { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Budget? Budget { get; set; }
    public virtual FinancialCategory? Category { get; set; }
}
