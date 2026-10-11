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
public class RecurringBillsController : Controller
{
    private readonly FamilyBudgetDbContext _dbContext;
    private readonly ICurrentFamilyContext _familyContext;
    private readonly IFinancialCalculationService _calcService;

    public RecurringBillsController(
        FamilyBudgetDbContext dbContext,
        ICurrentFamilyContext familyContext,
        IFinancialCalculationService calcService)
    {
        _dbContext = dbContext;
        _familyContext = familyContext;
        _calcService = calcService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var familyId = _familyContext.CurrentFamilyId!;

        var templates = await _dbContext.BillTemplates
            .Include(b => b.Category)
            .Include(b => b.ResponsibleMember).ThenInclude(m => m!.User)
            .Where(b => b.FamilyId == familyId)
            .OrderBy(b => b.NextDueDate)
            .ToListAsync();

        var occurrences = await _dbContext.BillOccurrences
            .Include(o => o.BillTemplate)
            .Where(o => o.FamilyId == familyId)
            .OrderBy(o => o.DueDate)
            .ToListAsync();

        var model = new BillTemplateListViewModel
        {
            Templates = templates,
            UpcomingOccurrences = occurrences,
            TotalPendingThisMonth = occurrences
                .Where(o => o.Status == BillOccurrenceStatus.Pending || o.Status == BillOccurrenceStatus.Overdue)
                .Sum(o => o.ExpectedAmount - o.ActualAmountPaid),
            CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹"
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var categories = await _dbContext.FinancialCategories
            .Where(c => c.IsSystemDefault || c.FamilyId == familyId)
            .Where(c => c.Type == CategoryType.Expense && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var members = await _dbContext.FamilyMemberships
            .Include(m => m.User)
            .Where(m => m.FamilyId == familyId && m.IsActive)
            .ToListAsync();

        var model = new CreateBillTemplateViewModel
        {
            BillingStartDate = DateTime.UtcNow.Date,
            NextDueDate = DateTime.UtcNow.Date.AddDays(7),
            Categories = categories,
            Members = members,
            CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBillTemplateViewModel model)
    {
        var familyId = _familyContext.CurrentFamilyId!;

        if (!ModelState.IsValid)
        {
            model.Categories = await _dbContext.FinancialCategories.Where(c => (c.IsSystemDefault || c.FamilyId == familyId) && c.Type == CategoryType.Expense).ToListAsync();
            model.Members = await _dbContext.FamilyMemberships.Include(m => m.User).Where(m => m.FamilyId == familyId && m.IsActive).ToListAsync();
            model.CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹";
            return View(model);
        }

        var template = new BillTemplate
        {
            FamilyId = familyId,
            BillName = model.BillName.Trim(),
            Payee = model.Payee.Trim(),
            CategoryId = model.CategoryId,
            ExpectedAmount = model.ExpectedAmount,
            IsVariableAmount = model.IsVariableAmount,
            Frequency = model.Frequency,
            BillingStartDate = model.BillingStartDate.ToUniversalTime(),
            NextDueDate = model.NextDueDate.ToUniversalTime(),
            EndDate = model.EndDate?.ToUniversalTime(),
            ResponsibleMemberId = string.IsNullOrWhiteSpace(model.ResponsibleMemberId) ? null : model.ResponsibleMemberId,
            AutopayEnabled = model.AutopayEnabled,
            Notes = model.Notes?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.BillTemplates.Add(template);

        // Generate immediate first occurrence
        var occurrence = new BillOccurrence
        {
            BillTemplate = template,
            FamilyId = familyId,
            DueDate = model.NextDueDate.ToUniversalTime(),
            ExpectedAmount = model.ExpectedAmount,
            ActualAmountPaid = 0m,
            Status = model.NextDueDate.Date < DateTime.UtcNow.Date ? BillOccurrenceStatus.Overdue : BillOccurrenceStatus.Pending
        };
        _dbContext.BillOccurrences.Add(occurrence);

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Recurring bill '{template.BillName}' configured.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> MarkPaid(string id)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var occurrence = await _dbContext.BillOccurrences
            .Include(o => o.BillTemplate)
            .FirstOrDefaultAsync(o => o.Id == id && o.FamilyId == familyId);

        if (occurrence == null) return NotFound();

        var members = await _dbContext.FamilyMemberships
            .Include(m => m.User)
            .Where(m => m.FamilyId == familyId && m.IsActive)
            .ToListAsync();

        var model = new MarkBillPaidViewModel
        {
            OccurrenceId = occurrence.Id,
            Occurrence = occurrence,
            ActualAmountPaid = occurrence.ExpectedAmount - occurrence.ActualAmountPaid,
            PaidDate = DateTime.UtcNow.Date,
            PaidByMemberId = occurrence.BillTemplate?.ResponsibleMemberId ?? members.FirstOrDefault()?.Id ?? string.Empty,
            PaymentMethod = "UPI",
            CreateExpenseEntry = true,
            Members = members,
            CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(MarkBillPaidViewModel model)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var occurrence = await _dbContext.BillOccurrences
            .Include(o => o.BillTemplate)
            .FirstOrDefaultAsync(o => o.Id == model.OccurrenceId && o.FamilyId == familyId);

        if (occurrence == null) return NotFound();

        if (!ModelState.IsValid)
        {
            model.Occurrence = occurrence;
            model.Members = await _dbContext.FamilyMemberships.Include(m => m.User).Where(m => m.FamilyId == familyId && m.IsActive).ToListAsync();
            model.CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹";
            return View(model);
        }

        occurrence.ActualAmountPaid += model.ActualAmountPaid;
        occurrence.PaidDate = model.PaidDate.ToUniversalTime();
        occurrence.Status = occurrence.ActualAmountPaid >= occurrence.ExpectedAmount
            ? BillOccurrenceStatus.Paid
            : BillOccurrenceStatus.PartiallyPaid;
        occurrence.Notes = model.Notes?.Trim();

        // Optionally create expense ledger entry
        if (model.CreateExpenseEntry && occurrence.BillTemplate != null)
        {
            var expense = new ExpenseTransaction
            {
                FamilyId = familyId,
                Title = $"Bill: {occurrence.BillTemplate.BillName}",
                Amount = model.ActualAmountPaid,
                CategoryId = occurrence.BillTemplate.CategoryId,
                PaidByMemberId = model.PaidByMemberId,
                ExpenseDate = model.PaidDate.ToUniversalTime(),
                PaymentMethod = model.PaymentMethod,
                MerchantOrShop = occurrence.BillTemplate.Payee,
                Notes = $"Settlement for bill occurrence due on {occurrence.DueDate:dd MMM yyyy}",
                CreatedByUserId = _familyContext.UserId!,
                CreatedAt = DateTime.UtcNow,
                Status = TransactionStatus.Confirmed
            };
            _dbContext.ExpenseTransactions.Add(expense);
            await _dbContext.SaveChangesAsync();

            occurrence.ExpenseTransactionId = expense.Id;
        }

        // Advance bill template next due date if paid
        if (occurrence.Status == BillOccurrenceStatus.Paid && occurrence.BillTemplate != null)
        {
            occurrence.BillTemplate.NextDueDate = _calcService.CalculateNextBillDueDate(
                occurrence.BillTemplate.NextDueDate,
                occurrence.BillTemplate.Frequency);
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            FamilyId = familyId,
            UserId = _familyContext.UserId,
            ActionType = "SETTLE_BILL",
            EntityName = "BillOccurrence",
            EntityId = occurrence.Id,
            Details = $"Settled bill payment of {model.ActualAmountPaid:N2} for '{occurrence.BillTemplate?.BillName}'"
        });

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Bill payment of {_familyContext.CurrentFamily?.CurrencySymbol}{model.ActualAmountPaid:N2} settled.";
        return RedirectToAction(nameof(Index));
    }
}
