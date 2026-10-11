using FamilyBudget.Application.Interfaces;
using FamilyBudget.Domain.Entities;
using FamilyBudget.Domain.Enums;
using FamilyBudget.Infrastructure.Data;
using FamilyBudget.Web.Filters;
using FamilyBudget.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Web.Controllers;

[Authorize]
[RequireFamily]
public class IncomeController : Controller
{
    private readonly FamilyBudgetDbContext _dbContext;
    private readonly ICurrentFamilyContext _familyContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public IncomeController(
        FamilyBudgetDbContext dbContext,
        ICurrentFamilyContext familyContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _familyContext = familyContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month, string? source, string? memberId)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var query = _dbContext.IncomeTransactions
            .Include(i => i.Category)
            .Include(i => i.RecipientMember).ThenInclude(m => m!.User)
            .Where(i => i.FamilyId == familyId);

        if (year.HasValue)
        {
            query = query.Where(i => i.DateReceived.Year == year.Value);
        }
        if (month.HasValue)
        {
            query = query.Where(i => i.DateReceived.Month == month.Value);
        }
        if (!string.IsNullOrWhiteSpace(source))
        {
            query = query.Where(i => i.IncomeSource == source);
        }
        if (!string.IsNullOrWhiteSpace(memberId))
        {
            query = query.Where(i => i.RecipientMemberId == memberId);
        }

        var incomes = await query
            .OrderByDescending(i => i.DateReceived)
            .ThenByDescending(i => i.CreatedAt)
            .ToListAsync();

        var members = await _dbContext.FamilyMemberships
            .Include(m => m.User)
            .Where(m => m.FamilyId == familyId && m.IsActive)
            .ToListAsync();

        var model = new IncomeListViewModel
        {
            Incomes = incomes,
            TotalFilteredAmount = incomes.Where(i => i.Status == TransactionStatus.Confirmed).Sum(i => i.Amount),
            SelectedYear = year,
            SelectedMonth = month,
            SelectedSource = source,
            SelectedMemberId = memberId,
            Members = members,
            CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹"
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var members = await _dbContext.FamilyMemberships
            .Include(m => m.User)
            .Where(m => m.FamilyId == familyId && m.IsActive)
            .ToListAsync();

        var categories = await _dbContext.FinancialCategories
            .Where(c => c.IsSystemDefault || c.FamilyId == familyId)
            .Where(c => c.Type == CategoryType.Income && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var currentMember = members.FirstOrDefault(m => m.UserId == _familyContext.UserId);

        var model = new CreateIncomeViewModel
        {
            DateReceived = DateTime.UtcNow.Date,
            RecipientMemberId = currentMember?.Id ?? members.FirstOrDefault()?.Id ?? string.Empty,
            Members = members,
            Categories = categories,
            CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateIncomeViewModel model)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var userId = _userManager.GetUserId(User)!;

        if (!ModelState.IsValid)
        {
            model.Members = await _dbContext.FamilyMemberships.Include(m => m.User).Where(m => m.FamilyId == familyId && m.IsActive).ToListAsync();
            model.Categories = await _dbContext.FinancialCategories.Where(c => (c.IsSystemDefault || c.FamilyId == familyId) && c.Type == CategoryType.Income).ToListAsync();
            model.CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹";
            return View(model);
        }

        var income = new IncomeTransaction
        {
            FamilyId = familyId,
            Title = model.Title.Trim(),
            Amount = model.Amount,
            IncomeSource = model.IncomeSource,
            RecipientMemberId = model.RecipientMemberId,
            DateReceived = model.DateReceived.ToUniversalTime(),
            PaymentMethod = model.PaymentMethod,
            AccountOrSource = model.AccountOrSource?.Trim(),
            CategoryId = model.CategoryId,
            Notes = model.Notes?.Trim(),
            IsRecurring = model.IsRecurring,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            Status = TransactionStatus.Confirmed
        };

        _dbContext.IncomeTransactions.Add(income);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            FamilyId = familyId,
            UserId = userId,
            ActionType = "CREATE",
            EntityName = "IncomeTransaction",
            EntityId = income.Id,
            Details = $"Recorded income '{income.Title}' of {income.Amount:N2}"
        });

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Income of {_familyContext.CurrentFamily?.CurrencySymbol}{income.Amount:N2} recorded successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var income = await _dbContext.IncomeTransactions.FirstOrDefaultAsync(i => i.Id == id && i.FamilyId == familyId);

        if (income == null) return NotFound();

        var isOwnerOrAdmin = _familyContext.CurrentRole == FamilyRole.Owner || _familyContext.CurrentRole == FamilyRole.Admin;
        if (!isOwnerOrAdmin && income.CreatedByUserId != _familyContext.UserId)
        {
            TempData["ErrorMessage"] = "You do not have permission to delete this income entry.";
            return RedirectToAction(nameof(Index));
        }

        _dbContext.IncomeTransactions.Remove(income);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            FamilyId = familyId,
            UserId = _familyContext.UserId,
            ActionType = "DELETE",
            EntityName = "IncomeTransaction",
            EntityId = id,
            Details = $"Deleted income '{income.Title}' for {income.Amount:N2}"
        });

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Income entry deleted.";
        return RedirectToAction(nameof(Index));
    }
}
