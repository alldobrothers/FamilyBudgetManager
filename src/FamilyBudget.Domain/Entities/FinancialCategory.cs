using FamilyBudget.Domain.Enums;

namespace FamilyBudget.Domain.Entities;

public class FinancialCategory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? FamilyId { get; set; } // Null for system defaults, set for custom family categories
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ColorHex { get; set; } = "#3B82F6";
    public string IconClass { get; set; } = "bi-tag";
    public CategoryType Type { get; set; } = CategoryType.Expense;
    public bool IsSystemDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Family? Family { get; set; }
    public virtual ICollection<ExpenseTransaction> Expenses { get; set; } = new List<ExpenseTransaction>();
    public virtual ICollection<IncomeTransaction> Incomes { get; set; } = new List<IncomeTransaction>();
}
