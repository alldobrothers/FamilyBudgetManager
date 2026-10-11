using FamilyBudget.Application.Interfaces;
using FamilyBudget.Domain.Entities;
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
public class BudgetsController : Controller
{
    private readonly FamilyBudgetDbContext _dbContext;
    private readonly ICurrentFamilyContext _familyContext;

    public BudgetsController(
        FamilyBudgetDbContext dbContext,
        ICurrentFamilyContext familyContext)
    {
        _dbContext = dbContext;
        _familyContext = familyContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var now = DateTime.UtcNow;
        var selectedYear = year ?? now.Year;
        var selectedMonth = month ?? now.Month;

        var periodStart = new DateTime(selectedYear, selectedMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1).AddTicks(-1);

        // Load existing budget
        var budget = await _dbContext.Budgets
            .Include(b => b.CategoryAllocations)
            .FirstOrDefaultAsync(b => b.FamilyId == familyId && b.PeriodYear == selectedYear && b.PeriodMonth == selectedMonth);

        // Load all active expense categories
        var expenseCategories = await _dbContext.FinancialCategories
            .Where(c => c.IsSystemDefault || c.FamilyId == familyId)
            .Where(c => c.Type == CategoryType.Expense && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        // Load actual spending for this month per category
        var actualExpenses = await _dbContext.ExpenseTransactions
            .Where(e => e.FamilyId == familyId && e.Status == TransactionStatus.Confirmed && e.ExpenseDate >= periodStart && e.ExpenseDate <= periodEnd)
            .GroupBy(e => e.CategoryId)
            .Select(g => new { CategoryId = g.Key, TotalSpent = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.TotalSpent);

        var totalSpent = actualExpenses.Values.Sum();

        var categoryRows = expenseCategories.Select(c =>
        {
            var alloc = budget?.CategoryAllocations.FirstOrDefault(a => a.CategoryId == c.Id);
            var spent = actualExpenses.TryGetValue(c.Id, out var s) ? s : 0m;
            var planned = alloc?.PlannedAmount ?? 0m;

            return new BudgetCategoryRowViewModel
            {
                CategoryId = c.Id,
                CategoryName = c.Name,
                ColorHex = c.ColorHex,
                IconClass = c.IconClass,
                PlannedAmount = planned,
                SpentAmount = spent
            };
        }).ToList();

        var model = new BudgetPlannerViewModel
        {
            BudgetId = budget?.Id,
            FamilyId = familyId,
            Year = selectedYear,
            Month = selectedMonth,
            TotalPlannedBudget = budget?.TotalPlannedBudget ?? _familyContext.CurrentFamily?.MonthlyBudget ?? 0m,
            RolloverAmount = budget?.RolloverAmount ?? 0m,
            Notes = budget?.Notes,
            TotalActualSpent = totalSpent,
            CategoryAllocations = categoryRows,
            CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(BudgetPlannerViewModel model)
    {
        if (_familyContext.CurrentRole != FamilyRole.Owner && _familyContext.CurrentRole != FamilyRole.Admin)
        {
            TempData["ErrorMessage"] = "Only family owners or admins can modify budget plans.";
            return RedirectToAction(nameof(Index), new { year = model.Year, month = model.Month });
        }

        var familyId = _familyContext.CurrentFamilyId!;

        var budget = await _dbContext.Budgets
            .Include(b => b.CategoryAllocations)
            .FirstOrDefaultAsync(b => b.FamilyId == familyId && b.PeriodYear == model.Year && b.PeriodMonth == model.Month);

        if (budget == null)
        {
            budget = new Budget
            {
                FamilyId = familyId,
                PeriodYear = model.Year,
                PeriodMonth = model.Month,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Budgets.Add(budget);
        }

        budget.TotalPlannedBudget = model.TotalPlannedBudget;
        budget.RolloverAmount = model.RolloverAmount;
        budget.Notes = model.Notes?.Trim();
        budget.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        // Update category allocations
        foreach (var row in model.CategoryAllocations)
        {
            var existingAlloc = budget.CategoryAllocations.FirstOrDefault(a => a.CategoryId == row.CategoryId);
            if (existingAlloc != null)
            {
                existingAlloc.PlannedAmount = row.PlannedAmount;
            }
            else if (row.PlannedAmount > 0)
            {
                budget.CategoryAllocations.Add(new BudgetCategory
                {
                    BudgetId = budget.Id,
                    CategoryId = row.CategoryId,
                    PlannedAmount = row.PlannedAmount
                });
            }
        }

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Budget for {new DateTime(model.Year, model.Month, 1):MMMM yyyy} saved successfully.";
        return RedirectToAction(nameof(Index), new { year = model.Year, month = model.Month });
    }
}
