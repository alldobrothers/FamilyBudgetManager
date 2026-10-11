using System.ComponentModel.DataAnnotations;
using FamilyBudget.Domain.Entities;

namespace FamilyBudget.Web.Models;

public class BudgetPlannerViewModel
{
    public string? BudgetId { get; set; }
    public string FamilyId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Year")]
    public int Year { get; set; } = DateTime.UtcNow.Year;

    [Required]
    [Display(Name = "Month")]
    public int Month { get; set; } = DateTime.UtcNow.Month;

    [Required(ErrorMessage = "Total planned budget is required")]
    [Range(0, 100000000)]
    [Display(Name = "Total Monthly Budget")]
    public decimal TotalPlannedBudget { get; set; }

    [Display(Name = "Rollover From Previous Month")]
    [Range(0, 100000000)]
    public decimal RolloverAmount { get; set; }

    [Display(Name = "Budget Notes")]
    public string? Notes { get; set; }

    // Actual execution metrics for the month
    public decimal TotalActualSpent { get; set; }
    public decimal RemainingAmount => Math.Max(0.0m, TotalPlannedBudget - TotalActualSpent);
    public decimal OverallUtilizationPercentage => TotalPlannedBudget > 0 ? Math.Round((TotalActualSpent / TotalPlannedBudget) * 100.0m, 1) : 0;

    public List<BudgetCategoryRowViewModel> CategoryAllocations { get; set; } = new();
    public string CurrencySymbol { get; set; } = "₹";
}

public class BudgetCategoryRowViewModel
{
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#3B82F6";
    public string IconClass { get; set; } = "bi-tag";

    [Range(0, 100000000)]
    public decimal PlannedAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal RemainingAmount => Math.Max(0.0m, PlannedAmount - SpentAmount);
    public decimal UtilizationPercentage => PlannedAmount > 0 ? Math.Round((SpentAmount / PlannedAmount) * 100.0m, 1) : 0;

    public string StatusBadgeClass => UtilizationPercentage switch
    {
        >= 100 => "bg-danger",
        >= 90 => "bg-warning text-dark",
        >= 75 => "bg-warning-subtle text-warning-emphasis",
        _ => "bg-success"
    };
}
