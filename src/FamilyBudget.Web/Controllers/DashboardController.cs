using FamilyBudget.Application.Interfaces;
using FamilyBudget.Domain.Enums;
using FamilyBudget.Infrastructure.Data;
using FamilyBudget.Web.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Web.Controllers;

[Authorize]
[RequireFamily]
public class DashboardController : Controller
{
    private readonly FamilyBudgetDbContext _dbContext;
    private readonly ICurrentFamilyContext _familyContext;
    private readonly IFinancialCalculationService _calcService;

    public DashboardController(
        FamilyBudgetDbContext dbContext,
        ICurrentFamilyContext familyContext,
        IFinancialCalculationService calcService)
    {
        _dbContext = dbContext;
        _familyContext = familyContext;
        _calcService = calcService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var family = _familyContext.CurrentFamily!;

        var now = DateTime.UtcNow;
        var selectedYear = year ?? now.Year;
        var selectedMonth = month ?? now.Month;

        var periodStart = new DateTime(selectedYear, selectedMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1).AddTicks(-1);

        // 1. Fetch Incomes in period
        var incomes = await _dbContext.IncomeTransactions
            .Include(i => i.Category)
            .Include(i => i.RecipientMember).ThenInclude(m => m!.User)
            .Where(i => i.FamilyId == familyId && i.DateReceived >= periodStart && i.DateReceived <= periodEnd)
            .ToListAsync();

        // 2. Fetch Expenses in period
        var expenses = await _dbContext.ExpenseTransactions
            .Include(e => e.Category)
            .Include(e => e.PaidByMember).ThenInclude(m => m!.User)
            .Include(e => e.Splits)
            .Where(e => e.FamilyId == familyId && e.ExpenseDate >= periodStart && e.ExpenseDate <= periodEnd)
            .ToListAsync();

        // 3. Fetch Budget for the period
        var budget = await _dbContext.Budgets
            .Include(b => b.CategoryAllocations).ThenInclude(ca => ca.Category)
            .FirstOrDefaultAsync(b => b.FamilyId == familyId && b.PeriodYear == selectedYear && b.PeriodMonth == selectedMonth);

        // 4. Fetch Debts & Repayments
        var debts = await _dbContext.Debts
            .Include(d => d.Repayments)
            .Where(d => d.FamilyId == familyId)
            .ToListAsync();

        // 5. Fetch Upcoming Bill Occurrences
        var upcomingBills = await _dbContext.BillOccurrences
            .Include(b => b.BillTemplate)
            .Where(b => b.FamilyId == familyId)
            .ToListAsync();

        // 6. Fetch Pending Obligations
        var pendingObligations = await _dbContext.PaymentObligations
            .Where(o => o.FamilyId == familyId)
            .ToListAsync();

        // 7. Fetch Active Members
        var members = await _dbContext.FamilyMemberships
            .Include(m => m.User)
            .Where(m => m.FamilyId == familyId && m.IsActive)
            .ToListAsync();

        // 8. Calculate Dashboard Summary using pure financial engine
        var dashboardData = _calcService.CalculateDashboardMetrics(
            incomes,
            expenses,
            budget,
            debts,
            upcomingBills,
            pendingObligations,
            members,
            family.CurrencyCode,
            family.CurrencySymbol,
            periodStart,
            periodEnd);

        // 9. Build 6-Month Spending Trend for Chart
        var trendPoints = new List<Application.DTOs.MonthlyTrendPointDto>();
        for (int i = 5; i >= 0; i--)
        {
            var dt = periodStart.AddMonths(-i);
            var mStart = new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var mEnd = mStart.AddMonths(1).AddTicks(-1);

            var mIncome = await _dbContext.IncomeTransactions
                .Where(t => t.FamilyId == familyId && t.Status == TransactionStatus.Confirmed && t.DateReceived >= mStart && t.DateReceived <= mEnd)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var mExpense = await _dbContext.ExpenseTransactions
                .Where(t => t.FamilyId == familyId && t.Status == TransactionStatus.Confirmed && t.ExpenseDate >= mStart && t.ExpenseDate <= mEnd)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            trendPoints.Add(new Application.DTOs.MonthlyTrendPointDto
            {
                MonthName = dt.ToString("MMM yyyy"),
                Year = dt.Year,
                Month = dt.Month,
                Income = mIncome,
                Expenses = mExpense
            });
        }
        dashboardData.MonthlySpendingTrend = trendPoints;

        ViewBag.SelectedYear = selectedYear;
        ViewBag.SelectedMonth = selectedMonth;

        return View(dashboardData);
    }
}
