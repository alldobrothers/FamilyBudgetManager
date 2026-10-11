using System.Text;
using FamilyBudget.Application.DTOs;
using FamilyBudget.Application.Interfaces;
using FamilyBudget.Domain.Enums;
using FamilyBudget.Infrastructure.Data;
using FamilyBudget.Web.Filters;
using FamilyBudget.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Web.Controllers;

[Authorize]
[RequireFamily]
public class ReportsController : Controller
{
    private readonly FamilyBudgetDbContext _dbContext;
    private readonly ICurrentFamilyContext _familyContext;
    private readonly IFinancialCalculationService _calcService;

    public ReportsController(
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
        var now = DateTime.UtcNow;
        var selectedYear = year ?? now.Year;
        var selectedMonth = month ?? now.Month;

        var startDate = new DateTime(selectedYear, selectedMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        var incomes = await _dbContext.IncomeTransactions
            .Include(i => i.Category)
            .Include(i => i.RecipientMember).ThenInclude(m => m!.User)
            .Where(i => i.FamilyId == familyId && i.DateReceived >= startDate && i.DateReceived <= endDate)
            .ToListAsync();

        var expenses = await _dbContext.ExpenseTransactions
            .Include(e => e.Category)
            .Include(e => e.PaidByMember).ThenInclude(m => m!.User)
            .Where(e => e.FamilyId == familyId && e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
            .ToListAsync();

        var members = await _dbContext.FamilyMemberships
            .Include(m => m.User)
            .Where(m => m.FamilyId == familyId && m.IsActive)
            .ToListAsync();

        var totalIncome = _calcService.CalculateTotalIncome(incomes);
        var totalExpenses = _calcService.CalculateTotalExpenses(expenses);

        // Category breakdown
        var categoryExpenses = expenses
            .Where(e => e.Status == TransactionStatus.Confirmed && e.Category != null)
            .GroupBy(e => new { e.Category!.Name, e.Category.ColorHex, e.Category.IconClass })
            .Select(g => new CategoryExpenseShareDto
            {
                CategoryName = g.Key.Name,
                ColorHex = g.Key.ColorHex,
                IconClass = g.Key.IconClass,
                Amount = g.Sum(x => x.Amount),
                Percentage = totalExpenses > 0 ? Math.Round((g.Sum(x => x.Amount) / totalExpenses) * 100.0m, 1) : 0
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        // Member breakdown
        var memberBreakdown = _calcService.CalculateMemberBreakdown(expenses, incomes, members);

        // 6-Month history
        var history = new List<MonthlyTrendPointDto>();
        for (int i = 5; i >= 0; i--)
        {
            var dt = startDate.AddMonths(-i);
            var mStart = new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var mEnd = mStart.AddMonths(1).AddTicks(-1);

            var mIncome = await _dbContext.IncomeTransactions
                .Where(t => t.FamilyId == familyId && t.Status == TransactionStatus.Confirmed && t.DateReceived >= mStart && t.DateReceived <= mEnd)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var mExpense = await _dbContext.ExpenseTransactions
                .Where(t => t.FamilyId == familyId && t.Status == TransactionStatus.Confirmed && t.ExpenseDate >= mStart && t.ExpenseDate <= mEnd)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            history.Add(new MonthlyTrendPointDto
            {
                MonthName = dt.ToString("MMM yyyy"),
                Year = dt.Year,
                Month = dt.Month,
                Income = mIncome,
                Expenses = mExpense
            });
        }

        var model = new FinancialReportViewModel
        {
            Year = selectedYear,
            Month = selectedMonth,
            StartDate = startDate,
            EndDate = endDate,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            CategoryBreakdown = categoryExpenses,
            MemberBreakdown = memberBreakdown,
            MonthlyHistory = history,
            CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹",
            CurrencyCode = _familyContext.CurrentFamily?.CurrencyCode ?? "INR"
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(int? year, int? month)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var now = DateTime.UtcNow;
        var selectedYear = year ?? now.Year;
        var selectedMonth = month ?? now.Month;

        var startDate = new DateTime(selectedYear, selectedMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        var expenses = await _dbContext.ExpenseTransactions
            .Include(e => e.Category)
            .Include(e => e.PaidByMember).ThenInclude(m => m!.User)
            .Where(e => e.FamilyId == familyId && e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync();

        var incomes = await _dbContext.IncomeTransactions
            .Include(i => i.Category)
            .Include(i => i.RecipientMember).ThenInclude(m => m!.User)
            .Where(i => i.FamilyId == familyId && i.DateReceived >= startDate && i.DateReceived <= endDate)
            .OrderByDescending(i => i.DateReceived)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Type,Date,Title,Category,Amount,Currency,PaymentMethod,Party,Notes,Status");

        foreach (var exp in expenses)
        {
            var memberName = exp.PaidByMember?.User?.DisplayName ?? exp.PaidByMember?.User?.FullName ?? "";
            sb.AppendLine($"Expense,{exp.ExpenseDate:yyyy-MM-dd},\"{EscapeCsv(exp.Title)}\",\"{EscapeCsv(exp.Category?.Name ?? "")}\",{exp.Amount:F2},{_familyContext.CurrentFamily?.CurrencyCode},\"{EscapeCsv(exp.PaymentMethod)}\",\"{EscapeCsv(memberName)}\",\"{EscapeCsv(exp.Notes ?? "")}\",{exp.Status}");
        }

        foreach (var inc in incomes)
        {
            var memberName = inc.RecipientMember?.User?.DisplayName ?? inc.RecipientMember?.User?.FullName ?? "";
            sb.AppendLine($"Income,{inc.DateReceived:yyyy-MM-dd},\"{EscapeCsv(inc.Title)}\",\"{EscapeCsv(inc.Category?.Name ?? inc.IncomeSource)}\",{inc.Amount:F2},{_familyContext.CurrentFamily?.CurrencyCode},\"{EscapeCsv(inc.PaymentMethod)}\",\"{EscapeCsv(memberName)}\",\"{EscapeCsv(inc.Notes ?? "")}\",{inc.Status}");
        }

        var fileName = $"FamilyBudget_{_familyContext.CurrentFamily?.Name.Replace(" ", "_")}_{selectedYear}_{selectedMonth:D2}.csv";
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());

        return File(bytes, "text/csv", fileName);
    }

    private static string EscapeCsv(string val)
    {
        return val.Replace("\"", "\"\"");
    }
}
