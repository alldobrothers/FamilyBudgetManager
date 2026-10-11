using System.ComponentModel.DataAnnotations;
using FamilyBudget.Domain.Entities;
using FamilyBudget.Domain.Enums;

namespace FamilyBudget.Web.Models;

public class ObligationListViewModel
{
    public List<PaymentObligation> Obligations { get; set; } = new();
    public decimal TotalPendingObligations { get; set; }
    public string CurrencySymbol { get; set; } = "₹";
}

public class CreateObligationViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(150)]
    [Display(Name = "Obligation Title / Description")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Amount due is required")]
    [Range(0.01, 100000000)]
    [Display(Name = "Amount Due")]
    public decimal AmountDue { get; set; }

    [Required(ErrorMessage = "Payee name is required")]
    [StringLength(150)]
    [Display(Name = "Payee / Beneficiary Name")]
    public string PayeeName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Due Date")]
    public DateTime DueDate { get; set; } = DateTime.UtcNow.Date.AddDays(7);

    [Required]
    [Display(Name = "Obligation Type")]
    public ObligationType ObligationType { get; set; } = ObligationType.HouseholdExpense;

    [Display(Name = "Responsible Member")]
    public string? ResponsibleMemberId { get; set; }

    [Required]
    [Display(Name = "Priority")]
    public ObligationPriority Priority { get; set; } = ObligationPriority.Medium;

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    public List<FamilyMembership> Members { get; set; } = new();
    public string CurrencySymbol { get; set; } = "₹";
}

public class SettleObligationViewModel
{
    [Required]
    public string ObligationId { get; set; } = string.Empty;

    public PaymentObligation? Obligation { get; set; }

    [Required(ErrorMessage = "Payment amount is required")]
    [Range(0.01, 100000000)]
    [Display(Name = "Amount Paid")]
    public decimal AmountPaid { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Settlement Date")]
    public DateTime SettlementDate { get; set; } = DateTime.UtcNow.Date;

    [Display(Name = "Settled By Family Member")]
    public string? SettledByMemberId { get; set; }

    [Display(Name = "Record as an Expense in Ledger?")]
    public bool RecordAsExpense { get; set; } = true;

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    public List<FamilyMembership> Members { get; set; } = new();
    public string CurrencySymbol { get; set; } = "₹";
}
