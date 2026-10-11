using System.ComponentModel.DataAnnotations;
using FamilyBudget.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace FamilyBudget.Web.Models;

public class IncomeListViewModel
{
    public List<IncomeTransaction> Incomes { get; set; } = new();
    public decimal TotalFilteredAmount { get; set; }
    public int? SelectedYear { get; set; }
    public int? SelectedMonth { get; set; }
    public string? SelectedSource { get; set; }
    public string? SelectedMemberId { get; set; }

    public List<FamilyMembership> Members { get; set; } = new();
    public string CurrencySymbol { get; set; } = "₹";
}

public class CreateIncomeViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(150)]
    [Display(Name = "Income Description / Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, 100000000, ErrorMessage = "Amount must be greater than zero")]
    [Display(Name = "Amount")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Income source is required")]
    [Display(Name = "Income Source")]
    public string IncomeSource { get; set; } = "Salary"; // Salary, Business, Freelance, Rental, Investment, Gift, Other

    [Required(ErrorMessage = "Recipient member is required")]
    [Display(Name = "Recipient Member")]
    public string RecipientMemberId { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date Received")]
    public DateTime DateReceived { get; set; } = DateTime.UtcNow.Date;

    [Required]
    [Display(Name = "Payment / Deposit Method")]
    public string PaymentMethod { get; set; } = "Bank Transfer";

    [Display(Name = "Bank Account / Depository")]
    public string? AccountOrSource { get; set; }

    [Display(Name = "Category (Optional)")]
    public string? CategoryId { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Recurring Monthly Inflow?")]
    public bool IsRecurring { get; set; }

    public List<FinancialCategory> Categories { get; set; } = new();
    public List<FamilyMembership> Members { get; set; } = new();
    public string CurrencySymbol { get; set; } = "₹";
}
