using System.ComponentModel.DataAnnotations;
using FamilyBudget.Domain.Entities;
using FamilyBudget.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace FamilyBudget.Web.Models;

public class ExpenseListViewModel
{
    public List<ExpenseTransaction> Expenses { get; set; } = new();
    public decimal TotalFilteredAmount { get; set; }
    public int? SelectedYear { get; set; }
    public int? SelectedMonth { get; set; }
    public string? SelectedCategoryId { get; set; }
    public string? SelectedMemberId { get; set; }
    public string? SearchTerm { get; set; }

    public List<FinancialCategory> Categories { get; set; } = new();
    public List<FamilyMembership> Members { get; set; } = new();
    public string CurrencySymbol { get; set; } = "₹";
}

public class CreateExpenseViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Title or description is required")]
    [StringLength(150)]
    [Display(Name = "Expense Title / Item")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, 100000000, ErrorMessage = "Amount must be greater than zero")]
    [Display(Name = "Amount")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Please select a category")]
    [Display(Name = "Category")]
    public string CategoryId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select who paid")]
    [Display(Name = "Paid By Member")]
    public string PaidByMemberId { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Expense Date")]
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = "UPI"; // Cash, UPI, Credit Card, Debit Card, Net Banking

    [Display(Name = "Merchant / Vendor")]
    public string? MerchantOrShop { get; set; }

    [Display(Name = "Notes / Description")]
    public string? Notes { get; set; }

    [Display(Name = "Tags (comma separated)")]
    public string? Tags { get; set; }

    [Display(Name = "Receipt / Proof Document")]
    public IFormFile? ReceiptFile { get; set; }
    public string? ExistingReceiptPath { get; set; }

    // Split settings
    [Display(Name = "Split this expense amongst members?")]
    public bool EnableSplitting { get; set; }
    public SplitType SplitType { get; set; } = SplitType.Equal;
    public List<ExpenseSplitRowViewModel> Splits { get; set; } = new();

    // Select Lists
    public List<FinancialCategory> Categories { get; set; } = new();
    public List<FamilyMembership> Members { get; set; } = new();
    public string CurrencySymbol { get; set; } = "₹";
}

public class ExpenseSplitRowViewModel
{
    public string MemberId { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public bool IsIncluded { get; set; } = true;
    public decimal AllocatedAmount { get; set; }
}

public class ExpenseDetailsViewModel
{
    public ExpenseTransaction Expense { get; set; } = null!;
    public string CurrencySymbol { get; set; } = "₹";
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }
}
