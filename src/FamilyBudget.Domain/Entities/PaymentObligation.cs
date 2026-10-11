using FamilyBudget.Domain.Enums;

namespace FamilyBudget.Domain.Entities;

public class PaymentObligation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FamilyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; } = 0.0m;
    public DateTime DueDate { get; set; }
    public string? ResponsibleMemberId { get; set; }
    public string PayeeName { get; set; } = string.Empty;
    public ObligationType ObligationType { get; set; } = ObligationType.HouseholdExpense;
    public string? RelatedBillOccurrenceId { get; set; }
    public string? RelatedExpenseId { get; set; }
    public string? RelatedDebtId { get; set; }
    public ObligationPriority Priority { get; set; } = ObligationPriority.Medium;
    public ObligationStatus Status { get; set; } = ObligationStatus.Pending;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Computed remaining balance
    public decimal RemainingBalance => Math.Max(0.0m, AmountDue - AmountPaid);

    // Navigation properties
    public virtual Family? Family { get; set; }
    public virtual FamilyMembership? ResponsibleMember { get; set; }
    public virtual BillOccurrence? RelatedBillOccurrence { get; set; }
    public virtual ExpenseTransaction? RelatedExpense { get; set; }
    public virtual Debt? RelatedDebt { get; set; }
}
