using FamilyBudget.Domain.Enums;

namespace FamilyBudget.Domain.Entities;

public class Debt
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FamilyId { get; set; } = string.Empty;
    public DebtType DebtType { get; set; } = DebtType.Borrowed;
    public string CreditorOrDebtorName { get; set; } = string.Empty;
    public string? LinkedMemberId { get; set; }
    public decimal OriginalPrincipal { get; set; }
    public decimal CurrentBalance { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public decimal? InterestRate { get; set; } // Annual percentage if configured
    public InterestType InterestType { get; set; } = InterestType.None;
    public string? RepaymentFrequency { get; set; } // Monthly, One-Time, Flexible
    public decimal? InstallmentAmount { get; set; }
    public string? Notes { get; set; }
    public string? DocumentPath { get; set; }
    public DebtStatus Status { get; set; } = DebtStatus.Active;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Family? Family { get; set; }
    public virtual FamilyMembership? LinkedMember { get; set; }
    public virtual ApplicationUser? CreatedByUser { get; set; }
    public virtual ICollection<DebtRepayment> Repayments { get; set; } = new List<DebtRepayment>();
}
