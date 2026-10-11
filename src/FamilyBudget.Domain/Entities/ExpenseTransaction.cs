using FamilyBudget.Domain.Enums;

namespace FamilyBudget.Domain.Entities;

public class ExpenseTransaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FamilyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string PaidByMemberId { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
    public string PaymentMethod { get; set; } = "UPI"; // Cash, UPI, Credit Card, Debit Card, Net Banking
    public string? MerchantOrShop { get; set; }
    public string? Notes { get; set; }
    public string? ReceiptPath { get; set; }
    public string? Tags { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Confirmed;
    public bool NeedsApproval { get; set; } = false;
    public bool IsApproved { get; set; } = true;
    public string? ApprovedByUserId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Family? Family { get; set; }
    public virtual FinancialCategory? Category { get; set; }
    public virtual FamilyMembership? PaidByMember { get; set; }
    public virtual ApplicationUser? CreatedByUser { get; set; }
    public virtual ApplicationUser? ApprovedByUser { get; set; }
    public virtual ICollection<ExpenseSplit> Splits { get; set; } = new List<ExpenseSplit>();
}
