using FamilyBudget.Application.DTOs;

namespace FamilyBudget.Web.Models;

public class FinancialReportViewModel
{
    public int Year { get; set; } = DateTime.UtcNow.Year;
    public int Month { get; set; } = DateTime.UtcNow.Month;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetSavings => TotalIncome - TotalExpenses;
    public decimal SavingsRatePercentage => TotalIncome > 0 ? Math.Round((NetSavings / TotalIncome) * 100.0m, 1) : 0;

    public List<CategoryExpenseShareDto> CategoryBreakdown { get; set; } = new();
    public List<MemberSpendingShareDto> MemberBreakdown { get; set; } = new();
    public List<MonthlyTrendPointDto> MonthlyHistory { get; set; } = new();

    public string CurrencySymbol { get; set; } = "₹";
    public string CurrencyCode { get; set; } = "INR";
}
