using System.ComponentModel.DataAnnotations;
using FamilyBudget.Domain.Entities;
using FamilyBudget.Domain.Enums;

namespace FamilyBudget.Web.Models;

public class CreateFamilyViewModel
{
    [Required(ErrorMessage = "Family / Household name is required")]
    [StringLength(100, ErrorMessage = "Family name cannot exceed 100 characters")]
    [Display(Name = "Household Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Currency Code")]
    public string CurrencyCode { get; set; } = "INR";

    [Required]
    [Display(Name = "Currency Symbol")]
    public string CurrencySymbol { get; set; } = "₹";

    [Display(Name = "Monthly Planned Budget (Optional)")]
    [Range(0, 100000000, ErrorMessage = "Budget must be a positive number")]
    public decimal? MonthlyBudget { get; set; }

    [Display(Name = "Description / Notes")]
    public string? Description { get; set; }
}

public class FamilySettingsViewModel
{
    public string FamilyId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Household Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Currency Symbol")]
    public string CurrencySymbol { get; set; } = "₹";

    [Required]
    [Display(Name = "Currency Code")]
    public string CurrencyCode { get; set; } = "INR";

    [Range(1, 28, ErrorMessage = "Month start day must be between 1 and 28")]
    [Display(Name = "Month Start Day")]
    public int MonthStartDay { get; set; } = 1;

    [Display(Name = "Require Expense Approvals")]
    public bool EnableExpenseApprovals { get; set; }

    [Display(Name = "First Budget Warning Threshold (%)")]
    [Range(10, 100)]
    public decimal BudgetWarningThreshold1 { get; set; } = 75.0m;

    [Display(Name = "Second Budget Warning Threshold (%)")]
    [Range(10, 150)]
    public decimal BudgetWarningThreshold2 { get; set; } = 90.0m;

    [Display(Name = "Notify on Overdue Bills")]
    public bool NotifyOnOverdueBills { get; set; } = true;
}

public class InviteMemberViewModel
{
    public string FamilyId { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Recipient email is required")]
    [EmailAddress]
    [Display(Name = "Member Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Assigned Role")]
    public FamilyRole Role { get; set; } = FamilyRole.Member;
}

public class AcceptInviteViewModel
{
    public string Token { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string InvitedEmail { get; set; } = string.Empty;
    public FamilyRole Role { get; set; }
    public string InviterName { get; set; } = string.Empty;
    public bool IsExpired { get; set; }
    public bool IsAccepted { get; set; }
}

public class FamilyMembersViewModel
{
    public Family Family { get; set; } = null!;
    public List<FamilyMembership> Members { get; set; } = new();
    public List<FamilyInvitation> PendingInvitations { get; set; } = new();
    public FamilyRole UserRole { get; set; }
}
