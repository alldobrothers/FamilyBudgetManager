namespace FamilyBudget.Domain.Entities;

public class Family
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "INR";
    public string CurrencySymbol { get; set; } = "₹";
    public string Country { get; set; } = "India";
    public string TimeZoneId { get; set; } = "India Standard Time";
    public int MonthStartDay { get; set; } = 1;
    public decimal? MonthlyBudget { get; set; }
    public string? Description { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ApplicationUser? CreatedByUser { get; set; }
    public virtual FamilySettings? Settings { get; set; }
    public virtual ICollection<FamilyMembership> Memberships { get; set; } = new List<FamilyMembership>();
    public virtual ICollection<FamilyInvitation> Invitations { get; set; } = new List<FamilyInvitation>();
    public virtual ICollection<ExpenseTransaction> Expenses { get; set; } = new List<ExpenseTransaction>();
    public virtual ICollection<IncomeTransaction> Incomes { get; set; } = new List<IncomeTransaction>();
    public virtual ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    public virtual ICollection<SavingsGoal> SavingsGoals { get; set; } = new List<SavingsGoal>();
    public virtual ICollection<Debt> Debts { get; set; } = new List<Debt>();
    public virtual ICollection<BillTemplate> BillTemplates { get; set; } = new List<BillTemplate>();
    public virtual ICollection<PaymentObligation> Obligations { get; set; } = new List<PaymentObligation>();
    public virtual ICollection<FinancialCategory> CustomCategories { get; set; } = new List<FinancialCategory>();
}
