namespace FamilyBudget.Domain.Entities;

public class DebtRepayment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DebtId { get; set; } = string.Empty;
    public string? RepaidByMemberId { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalPortion { get; set; }
    public decimal InterestPortion { get; set; } = 0.0m;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string PaymentMethod { get; set; } = "UPI";
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Debt? Debt { get; set; }
    public virtual FamilyMembership? RepaidByMember { get; set; }
}
