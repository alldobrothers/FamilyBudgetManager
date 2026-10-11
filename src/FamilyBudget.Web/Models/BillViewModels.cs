using System.ComponentModel.DataAnnotations;
using FamilyBudget.Domain.Entities;
using FamilyBudget.Domain.Enums;

namespace FamilyBudget.Web.Models;

public class BillTemplateListViewModel
{
    public List<BillTemplate> Templates { get; set; } = new();
    public List<BillOccurrence> UpcomingOccurrences { get; set; } = new();
    public decimal TotalPendingThisMonth { get; set; }
    public string CurrencySymbol { get; set; } = "₹";
}

public class CreateBillTemplateViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Bill name is required")]
    [StringLength(120)]
    [Display(Name = "Bill / Utility Name")]
    public string BillName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Payee is required")]
    [StringLength(120)]
    [Display(Name = "Provider / Payee")]
    public string Payee { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required")]
    [Display(Name = "Category")]
    public string CategoryId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Expected amount is required")]
    [Range(0.01, 100000000)]
    [Display(Name = "Expected Amount")]
    public decimal ExpectedAmount { get; set; }

    [Display(Name = "Variable Amount (changes each cycle)")]
    public bool IsVariableAmount { get; set; }

    [Required]
    [Display(Name = "Billing Frequency")]
    public BillFrequency Frequency { get; set; } = BillFrequency.Monthly;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Billing Cycle Start Date")]
    public DateTime BillingStartDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "First Due Date")]
    public DateTime NextDueDate { get; set; } = DateTime.UtcNow.Date;

    [DataType(DataType.Date)]
    [Display(Name = "End Date (Optional)")]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Responsible Family Member")]
    public string? ResponsibleMemberId { get; set; }

    [Display(Name = "Auto-pay Enabled on Bank/App")]
    public bool AutopayEnabled { get; set; }

    [Display(Name = "Notes / Account Number")]
    public string? Notes { get; set; }

    public List<FinancialCategory> Categories { get; set; } = new();
    public List<FamilyMembership> Members { get; set; } = new();
    public string CurrencySymbol { get; set; } = "₹";
}

public class MarkBillPaidViewModel
{
    [Required]
    public string OccurrenceId { get; set; } = string.Empty;

    public BillOccurrence? Occurrence { get; set; }

    [Required(ErrorMessage = "Amount paid is required")]
    [Range(0.01, 100000000)]
    [Display(Name = "Actual Amount Paid")]
    public decimal ActualAmountPaid { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Payment Date")]
    public DateTime PaidDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    [Display(Name = "Paid By Family Member")]
    public string PaidByMemberId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = "UPI";

    [Display(Name = "Also record as a household Expense?")]
    public bool CreateExpenseEntry { get; set; } = true;

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    public List<FamilyMembership> Members { get; set; } = new();
    public string CurrencySymbol { get; set; } = "₹";
}
