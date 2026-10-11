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
public class DebtsController : Controller
{
    private readonly FamilyBudgetDbContext _dbContext;
    private readonly ICurrentFamilyContext _familyContext;
    private readonly IFinancialCalculationService _calcService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DebtsController(
        FamilyBudgetDbContext dbContext,
        ICurrentFamilyContext familyContext,
        IFinancialCalculationService calcService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _familyContext = familyContext;
        _calcService = calcService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var familyId = _familyContext.CurrentFamilyId!;

        var debts = await _dbContext.Debts
            .Include(d => d.LinkedMember).ThenInclude(m => m!.User)
            .Include(d => d.Repayments)
            .Where(d => d.FamilyId == familyId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        var borrowed = debts.Where(d => d.DebtType == DebtType.Borrowed).ToList();
        var lent = debts.Where(d => d.DebtType == DebtType.Lent).ToList();

        var model = new DebtListViewModel
        {
            BorrowedDebts = borrowed,
            LentDebts = lent,
            TotalBorrowedOutstanding = _calcService.CalculateOutstandingDebtBalance(borrowed),
            TotalLentOutstanding = lent.Where(d => d.Status != DebtStatus.PaidOff && d.Status != DebtStatus.Cancelled)
                .Sum(d => _calcService.CalculateRecalculatedDebtBalance(d.OriginalPrincipal, d.Repayments)),
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

        var model = new CreateDebtViewModel
        {
            StartDate = DateTime.UtcNow.Date,
            Members = members,
            CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDebtViewModel model)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var userId = _userManager.GetUserId(User)!;

        if (!ModelState.IsValid)
        {
            model.Members = await _dbContext.FamilyMemberships.Include(m => m.User).Where(m => m.FamilyId == familyId && m.IsActive).ToListAsync();
            model.CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹";
            return View(model);
        }

        var debt = new Debt
        {
            FamilyId = familyId,
            DebtType = model.DebtType,
            CreditorOrDebtorName = model.CreditorOrDebtorName.Trim(),
            LinkedMemberId = string.IsNullOrWhiteSpace(model.LinkedMemberId) ? null : model.LinkedMemberId,
            OriginalPrincipal = model.OriginalPrincipal,
            CurrentBalance = model.OriginalPrincipal,
            CurrencyCode = _familyContext.CurrentFamily?.CurrencyCode ?? "INR",
            StartDate = model.StartDate.ToUniversalTime(),
            DueDate = model.DueDate?.ToUniversalTime(),
            InterestRate = model.InterestRate,
            InterestType = model.InterestType,
            RepaymentFrequency = model.RepaymentFrequency,
            InstallmentAmount = model.InstallmentAmount,
            Notes = model.Notes?.Trim(),
            Status = DebtStatus.Active,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Debts.Add(debt);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            FamilyId = familyId,
            UserId = userId,
            ActionType = "CREATE",
            EntityName = "Debt",
            EntityId = debt.Id,
            Details = $"Recorded {(debt.DebtType == DebtType.Borrowed ? "borrowed debt" : "lent money")} '{debt.CreditorOrDebtorName}' for {debt.OriginalPrincipal:N2}"
        });

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Debt record for '{debt.CreditorOrDebtorName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Repay(string id)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var debt = await _dbContext.Debts
            .Include(d => d.Repayments)
            .FirstOrDefaultAsync(d => d.Id == id && d.FamilyId == familyId);

        if (debt == null) return NotFound();

        var members = await _dbContext.FamilyMemberships
            .Include(m => m.User)
            .Where(m => m.FamilyId == familyId && m.IsActive)
            .ToListAsync();

        var remaining = _calcService.CalculateRecalculatedDebtBalance(debt.OriginalPrincipal, debt.Repayments);

        var model = new RecordRepaymentViewModel
        {
            DebtId = debt.Id,
            Debt = debt,
            Amount = debt.InstallmentAmount.HasValue && debt.InstallmentAmount.Value > 0 ? Math.Min(debt.InstallmentAmount.Value, remaining) : remaining,
            PrincipalPortion = debt.InstallmentAmount.HasValue && debt.InstallmentAmount.Value > 0 ? Math.Min(debt.InstallmentAmount.Value, remaining) : remaining,
            InterestPortion = 0m,
            PaymentDate = DateTime.UtcNow.Date,
            RepaidByMemberId = debt.LinkedMemberId ?? members.FirstOrDefault()?.Id,
            Members = members,
            CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Repay(RecordRepaymentViewModel model)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var debt = await _dbContext.Debts
            .Include(d => d.Repayments)
            .FirstOrDefaultAsync(d => d.Id == model.DebtId && d.FamilyId == familyId);

        if (debt == null) return NotFound();

        if (!ModelState.IsValid)
        {
            model.Debt = debt;
            model.Members = await _dbContext.FamilyMemberships.Include(m => m.User).Where(m => m.FamilyId == familyId && m.IsActive).ToListAsync();
            model.CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹";
            return View(model);
        }

        var repayment = new DebtRepayment
        {
            DebtId = debt.Id,
            RepaidByMemberId = string.IsNullOrWhiteSpace(model.RepaidByMemberId) ? null : model.RepaidByMemberId,
            Amount = model.Amount,
            PrincipalPortion = model.PrincipalPortion,
            InterestPortion = model.InterestPortion,
            PaymentDate = model.PaymentDate.ToUniversalTime(),
            PaymentMethod = model.PaymentMethod,
            ReferenceNumber = model.ReferenceNumber?.Trim(),
            Notes = model.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        debt.Repayments.Add(repayment);

        // Recalculate balance
        var newBalance = _calcService.CalculateRecalculatedDebtBalance(debt.OriginalPrincipal, debt.Repayments);
        debt.CurrentBalance = newBalance;

        if (newBalance <= 0)
        {
            debt.Status = DebtStatus.PaidOff;
        }
        else
        {
            debt.Status = DebtStatus.PartiallyRepaid;
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            FamilyId = familyId,
            UserId = _familyContext.UserId,
            ActionType = "REPAY",
            EntityName = "DebtRepayment",
            EntityId = repayment.Id,
            Details = $"Recorded repayment of {repayment.Amount:N2} towards debt '{debt.CreditorOrDebtorName}'. New balance: {debt.CurrentBalance:N2}"
        });

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Repayment of {_familyContext.CurrentFamily?.CurrencySymbol}{repayment.Amount:N2} recorded. Remaining balance: {_familyContext.CurrentFamily?.CurrencySymbol}{debt.CurrentBalance:N2}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var debt = await _dbContext.Debts
            .Include(d => d.LinkedMember).ThenInclude(m => m!.User)
            .Include(d => d.Repayments).ThenInclude(r => r.RepaidByMember).ThenInclude(m => m!.User)
            .FirstOrDefaultAsync(d => d.Id == id && d.FamilyId == familyId);

        if (debt == null) return NotFound();

        ViewBag.CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹";
        return View(debt);
    }
}
