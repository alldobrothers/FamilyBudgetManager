using FamilyBudget.Domain.Enums;

namespace FamilyBudget.Domain.Entities;

public class FamilyMembership
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FamilyId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public FamilyRole Role { get; set; } = FamilyRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public decimal? AllocatedPersonalBudget { get; set; }
    public bool CanApproveExpenses { get; set; } = false;
    public bool CanManageBills { get; set; } = true;
    public bool CanViewDebts { get; set; } = true;
    public bool CanExportReports { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Family? Family { get; set; }
    public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<ExpenseTransaction> PaidExpenses { get; set; } = new List<ExpenseTransaction>();
    public virtual ICollection<IncomeTransaction> ReceivedIncomes { get; set; } = new List<IncomeTransaction>();
    public virtual ICollection<ExpenseSplit> ExpenseSplits { get; set; } = new List<ExpenseSplit>();
}
