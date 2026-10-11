namespace FamilyBudget.Domain.Entities;

public class Budget
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FamilyId { get; set; } = string.Empty;
    public int PeriodYear { get; set; } = DateTime.UtcNow.Year;
    public int PeriodMonth { get; set; } = DateTime.UtcNow.Month;
    public decimal TotalPlannedBudget { get; set; }
    public decimal RolloverAmount { get; set; } = 0.0m;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Family? Family { get; set; }
    public virtual ICollection<BudgetCategory> CategoryAllocations { get; set; } = new List<BudgetCategory>();
}
