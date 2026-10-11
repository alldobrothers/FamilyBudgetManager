using FamilyBudget.Domain.Enums;

namespace FamilyBudget.Domain.Entities;

public class BillTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FamilyId { get; set; } = string.Empty;
    public string BillName { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string Payee { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
    public bool IsVariableAmount { get; set; } = false;
    public BillFrequency Frequency { get; set; } = BillFrequency.Monthly;
    public DateTime BillingStartDate { get; set; } = DateTime.UtcNow;
    public DateTime NextDueDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public string? ResponsibleMemberId { get; set; }
    public bool AutopayEnabled { get; set; } = false;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Family? Family { get; set; }
    public virtual FinancialCategory? Category { get; set; }
    public virtual FamilyMembership? ResponsibleMember { get; set; }
    public virtual ICollection<BillOccurrence> Occurrences { get; set; } = new List<BillOccurrence>();
}
