using System.ComponentModel.DataAnnotations;
using FamilyBudget.Domain.Entities;
using FamilyBudget.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace FamilyBudget.Web.Models;

public class DebtListViewModel
{
    public List<Debt> BorrowedDebts { get; set; } = new();
    public List<Debt> LentDebts { get; set; } = new();
    public decimal TotalBorrowedOutstanding { get; set; }
    public decimal TotalLentOutstanding { get; set; }
    public string CurrencySymbol { get; set; } = "₹";
}

public class CreateDebtViewModel
{
    public string? Id { get; set; }

    [Required]
    [Display(Name = "Debt Type")]
    public DebtType DebtType { get; set; } = DebtType.Borrowed; // Borrowed (Liability) or Lent (Receivable)

    [Required(ErrorMessage = "Name of the person/institution is required")]
    [StringLength(150)]
    [Display(Name = "Lender / Borrower Name")]
    public string CreditorOrDebtorName { get; set; } = string.Empty;

    [Display(Name = "Linked Family Member (Optional)")]
    public string? LinkedMemberId { get; set; }

    [Required(ErrorMessage = "Principal amount is required")]
    [Range(0.01, 100000000, ErrorMessage = "Principal must be greater than zero")]
    [Display(Name = "Original Principal Amount")]
    public decimal OriginalPrincipal { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

    [DataType(DataType.Date)]
    [Display(Name = "Due Date (Optional)")]
    public DateTime? DueDate { get; set; }

    [Display(Name = "Interest Rate (% p.a.)")]
    [Range(0, 100)]
    public decimal? InterestRate { get; set; }

    [Display(Name = "Interest Type")]
    public InterestType InterestType { get; set; } = InterestType.None;

    [Display(Name = "Repayment Frequency")]
    public string? RepaymentFrequency { get; set; } = "Monthly"; // Monthly, One-Time, Flexible

    [Display(Name = "Expected Installment Amount")]
    [Range(0, 100000000)]
    public decimal? InstallmentAmount { get; set; }

    [Display(Name = "Notes / Purpose")]
    public string? Notes { get; set; }

    [Display(Name = "Supporting Document / Promissory Note")]
    public IFormFile? DocumentFile { get; set; }

    public List<FamilyMembership> Members { get; set; } = new();
    public string CurrencySymbol { get; set; } = "₹";
}

public class RecordRepaymentViewModel
{
    [Required]
    public string DebtId { get; set; } = string.Empty;

    public Debt? Debt { get; set; }

    [Required(ErrorMessage = "Repayment amount is required")]
    [Range(0.01, 100000000, ErrorMessage = "Amount must be greater than zero")]
    [Display(Name = "Total Repayment Amount")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Principal portion is required")]
    [Range(0, 100000000)]
    [Display(Name = "Principal Portion")]
    public decimal PrincipalPortion { get; set; }

    [Range(0, 100000000)]
    [Display(Name = "Interest Portion (if any)")]
    public decimal InterestPortion { get; set; } = 0.0m;

    [Display(Name = "Repaid By Family Member")]
    public string? RepaidByMemberId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Payment Date")]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = "UPI";

    [Display(Name = "Transaction Reference / UTR Number")]
    public string? ReferenceNumber { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    public List<FamilyMembership> Members { get; set; } = new();
    public string CurrencySymbol { get; set; } = "₹";
}
