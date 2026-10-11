using FamilyBudget.Domain.Enums;

namespace FamilyBudget.Domain.Entities;

public class BillOccurrence
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string BillTemplateId { get; set; } = string.Empty;
    public string FamilyId { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal ActualAmountPaid { get; set; } = 0.0m;
    public BillOccurrenceStatus Status { get; set; } = BillOccurrenceStatus.Pending;
    public DateTime? PaidDate { get; set; }
    public string? ExpenseTransactionId { get; set; } // If converted/linked to an expense
    public string? Notes { get; set; }

    // Navigation properties
    public virtual BillTemplate? BillTemplate { get; set; }
    public virtual Family? Family { get; set; }
    public virtual ExpenseTransaction? ExpenseTransaction { get; set; }
}
