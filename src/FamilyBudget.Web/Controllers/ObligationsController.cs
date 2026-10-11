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
public class ObligationsController : Controller
{
    private readonly FamilyBudgetDbContext _dbContext;
    private readonly ICurrentFamilyContext _familyContext;

    public ObligationsController(
        FamilyBudgetDbContext dbContext,
        ICurrentFamilyContext familyContext)
    {
        _dbContext = dbContext;
        _familyContext = familyContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var familyId = _familyContext.CurrentFamilyId!;

        var obligations = await _dbContext.PaymentObligations
            .Include(o => o.ResponsibleMember).ThenInclude(m => m!.User)
            .Where(o => o.FamilyId == familyId)
            .OrderBy(o => o.DueDate)
            .ToListAsync();

        var model = new ObligationListViewModel
        {
            Obligations = obligations,
            TotalPendingObligations = obligations
                .Where(o => o.Status != ObligationStatus.Paid && o.Status != ObligationStatus.Cancelled)
                .Sum(o => o.RemainingBalance),
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

        var model = new CreateObligationViewModel
        {
            DueDate = DateTime.UtcNow.Date.AddDays(7),
            Members = members,
            CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateObligationViewModel model)
    {
        var familyId = _familyContext.CurrentFamilyId!;

        if (!ModelState.IsValid)
        {
            model.Members = await _dbContext.FamilyMemberships.Include(m => m.User).Where(m => m.FamilyId == familyId && m.IsActive).ToListAsync();
            model.CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹";
            return View(model);
        }

        var obligation = new PaymentObligation
        {
            FamilyId = familyId,
            Title = model.Title.Trim(),
            AmountDue = model.AmountDue,
            AmountPaid = 0m,
            PayeeName = model.PayeeName.Trim(),
            DueDate = model.DueDate.ToUniversalTime(),
            ObligationType = model.ObligationType,
            ResponsibleMemberId = string.IsNullOrWhiteSpace(model.ResponsibleMemberId) ? null : model.ResponsibleMemberId,
            Priority = model.Priority,
            Status = model.DueDate.Date < DateTime.UtcNow.Date ? ObligationStatus.Overdue : ObligationStatus.Pending,
            Notes = model.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.PaymentObligations.Add(obligation);
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Payment obligation '{obligation.Title}' recorded.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Settle(string id)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var obligation = await _dbContext.PaymentObligations
            .FirstOrDefaultAsync(o => o.Id == id && o.FamilyId == familyId);

        if (obligation == null) return NotFound();

        var members = await _dbContext.FamilyMemberships
            .Include(m => m.User)
            .Where(m => m.FamilyId == familyId && m.IsActive)
            .ToListAsync();

        var model = new SettleObligationViewModel
        {
            ObligationId = obligation.Id,
            Obligation = obligation,
            AmountPaid = obligation.RemainingBalance,
            SettlementDate = DateTime.UtcNow.Date,
            SettledByMemberId = obligation.ResponsibleMemberId ?? members.FirstOrDefault()?.Id,
            RecordAsExpense = true,
            Members = members,
            CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settle(SettleObligationViewModel model)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var obligation = await _dbContext.PaymentObligations
            .FirstOrDefaultAsync(o => o.Id == model.ObligationId && o.FamilyId == familyId);

        if (obligation == null) return NotFound();

        if (!ModelState.IsValid)
        {
            model.Obligation = obligation;
            model.Members = await _dbContext.FamilyMemberships.Include(m => m.User).Where(m => m.FamilyId == familyId && m.IsActive).ToListAsync();
            model.CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹";
            return View(model);
        }

        obligation.AmountPaid += model.AmountPaid;
        obligation.Status = obligation.AmountPaid >= obligation.AmountDue
            ? ObligationStatus.Paid
            : ObligationStatus.PartiallyPaid;

        if (model.RecordAsExpense)
        {
            // Find or use default Misc category
            var cat = await _dbContext.FinancialCategories.FirstOrDefaultAsync(c => c.Name == "Miscellaneous")
                      ?? await _dbContext.FinancialCategories.FirstAsync();

            var expense = new ExpenseTransaction
            {
                FamilyId = familyId,
                Title = $"Settled: {obligation.Title}",
                Amount = model.AmountPaid,
                CategoryId = cat.Id,
                PaidByMemberId = model.SettledByMemberId ?? _dbContext.FamilyMemberships.First(m => m.FamilyId == familyId).Id,
                ExpenseDate = model.SettlementDate.ToUniversalTime(),
                PaymentMethod = "UPI",
                MerchantOrShop = obligation.PayeeName,
                Notes = $"Settlement payment for obligation: {obligation.Title}",
                CreatedByUserId = _familyContext.UserId!,
                CreatedAt = DateTime.UtcNow,
                Status = TransactionStatus.Confirmed
            };
            _dbContext.ExpenseTransactions.Add(expense);
            await _dbContext.SaveChangesAsync();

            obligation.RelatedExpenseId = expense.Id;
        }

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Payment obligation settlement of {_familyContext.CurrentFamily?.CurrencySymbol}{model.AmountPaid:N2} recorded.";
        return RedirectToAction(nameof(Index));
    }
}
