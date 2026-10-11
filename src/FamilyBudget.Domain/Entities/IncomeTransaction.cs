using FamilyBudget.Domain.Enums;

namespace FamilyBudget.Domain.Entities;

public class IncomeTransaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FamilyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string IncomeSource { get; set; } = "Salary"; // Salary, Freelance, Business, Rental, Dividend, Gift, Other
    public string RecipientMemberId { get; set; } = string.Empty;
    public DateTime DateReceived { get; set; } = DateTime.UtcNow;
    public string PaymentMethod { get; set; } = "Bank Transfer";
    public string? CategoryId { get; set; }
    public string? AccountOrSource { get; set; }
    public string? Notes { get; set; }
    public string? AttachmentPath { get; set; }
    public bool IsRecurring { get; set; } = false;
    public TransactionStatus Status { get; set; } = TransactionStatus.Confirmed;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Family? Family { get; set; }
    public virtual FinancialCategory? Category { get; set; }
    public virtual FamilyMembership? RecipientMember { get; set; }
    public virtual ApplicationUser? CreatedByUser { get; set; }
}
