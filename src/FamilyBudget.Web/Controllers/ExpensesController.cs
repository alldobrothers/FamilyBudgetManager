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
public class ExpensesController : Controller
{
    private readonly FamilyBudgetDbContext _dbContext;
    private readonly ICurrentFamilyContext _familyContext;
    private readonly IFinancialCalculationService _calcService;
    private readonly IFileStorageService _fileStorage;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(
        FamilyBudgetDbContext dbContext,
        ICurrentFamilyContext familyContext,
        IFinancialCalculationService calcService,
        IFileStorageService fileStorage,
        UserManager<ApplicationUser> userManager,
        ILogger<ExpensesController> logger)
    {
        _dbContext = dbContext;
        _familyContext = familyContext;
        _calcService = calcService;
        _fileStorage = fileStorage;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month, string? categoryId, string? memberId, string? search)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var query = _dbContext.ExpenseTransactions
            .Include(e => e.Category)
            .Include(e => e.PaidByMember).ThenInclude(m => m!.User)
            .Where(e => e.FamilyId == familyId);

        if (year.HasValue)
        {
            query = query.Where(e => e.ExpenseDate.Year == year.Value);
        }
        if (month.HasValue)
        {
            query = query.Where(e => e.ExpenseDate.Month == month.Value);
        }
        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            query = query.Where(e => e.CategoryId == categoryId);
        }
        if (!string.IsNullOrWhiteSpace(memberId))
        {
            query = query.Where(e => e.PaidByMemberId == memberId);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(s) || (e.MerchantOrShop != null && e.MerchantOrShop.ToLower().Contains(s)));
        }

        var expenses = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync();

        var categories = await _dbContext.FinancialCategories
            .Where(c => c.IsSystemDefault || c.FamilyId == familyId)
            .Where(c => c.Type == CategoryType.Expense)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var members = await _dbContext.FamilyMemberships
            .Include(m => m.User)
            .Where(m => m.FamilyId == familyId && m.IsActive)
            .ToListAsync();

        var model = new ExpenseListViewModel
        {
            Expenses = expenses,
            TotalFilteredAmount = expenses.Where(e => e.Status == TransactionStatus.Confirmed).Sum(e => e.Amount),
            SelectedYear = year,
            SelectedMonth = month,
            SelectedCategoryId = categoryId,
            SelectedMemberId = memberId,
            SearchTerm = search,
            Categories = categories,
            Members = members,
            CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹"
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var categories = await GetExpenseCategoriesAsync(familyId);
        var members = await GetFamilyMembersAsync(familyId);

        var currentMember = members.FirstOrDefault(m => m.UserId == _familyContext.UserId);

        var model = new CreateExpenseViewModel
        {
            ExpenseDate = DateTime.UtcNow.Date,
            PaidByMemberId = currentMember?.Id ?? members.FirstOrDefault()?.Id ?? string.Empty,
            Categories = categories,
            Members = members,
            CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹",
            Splits = members.Select(m => new ExpenseSplitRowViewModel
            {
                MemberId = m.Id,
                MemberName = m.User?.DisplayName ?? m.User?.FullName ?? "Member",
                IsIncluded = true,
                AllocatedAmount = 0m
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateExpenseViewModel model)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var userId = _userManager.GetUserId(User)!;

        if (!ModelState.IsValid)
        {
            model.Categories = await GetExpenseCategoriesAsync(familyId);
            model.Members = await GetFamilyMembersAsync(familyId);
            model.CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹";
            return View(model);
        }

        // Receipt upload handling
        string? receiptPath = null;
        if (model.ReceiptFile != null && model.ReceiptFile.Length > 0)
        {
            if (!_fileStorage.IsAllowedFileType(model.ReceiptFile.FileName))
            {
                ModelState.AddModelError("ReceiptFile", "Only JPG, PNG, WEBP, or PDF files are allowed.");
                model.Categories = await GetExpenseCategoriesAsync(familyId);
                model.Members = await GetFamilyMembersAsync(familyId);
                model.CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹";
                return View(model);
            }

            using var stream = model.ReceiptFile.OpenReadStream();
            receiptPath = await _fileStorage.SaveFileAsync(stream, model.ReceiptFile.FileName, "receipts");
        }

        // Settings check for approvals
        var settings = await _dbContext.FamilySettings.FirstOrDefaultAsync(s => s.FamilyId == familyId);
        bool requiresApproval = settings?.EnableExpenseApprovals == true &&
                                _familyContext.CurrentRole != FamilyRole.Owner &&
                                _familyContext.CurrentRole != FamilyRole.Admin;

        var expense = new ExpenseTransaction
        {
            FamilyId = familyId,
            Title = model.Title.Trim(),
            Amount = model.Amount,
            CategoryId = model.CategoryId,
            PaidByMemberId = model.PaidByMemberId,
            ExpenseDate = model.ExpenseDate.ToUniversalTime(),
            PaymentMethod = model.PaymentMethod,
            MerchantOrShop = model.MerchantOrShop?.Trim(),
            Notes = model.Notes?.Trim(),
            Tags = model.Tags?.Trim(),
            ReceiptPath = receiptPath,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            NeedsApproval = requiresApproval,
            IsApproved = !requiresApproval,
            Status = requiresApproval ? TransactionStatus.PendingApproval : TransactionStatus.Confirmed
        };

        _dbContext.ExpenseTransactions.Add(expense);
        await _dbContext.SaveChangesAsync();

        // Process splits if enabled
        if (model.EnableSplitting && model.Splits.Any(s => s.IsIncluded))
        {
            var included = model.Splits.Where(s => s.IsIncluded).ToList();
            if (model.SplitType == SplitType.Equal)
            {
                var perPerson = Math.Round(model.Amount / included.Count, 2);
                decimal runningSum = 0;
                for (int i = 0; i < included.Count; i++)
                {
                    decimal splitAmt = (i == included.Count - 1) ? (model.Amount - runningSum) : perPerson;
                    runningSum += splitAmt;

                    _dbContext.ExpenseSplits.Add(new ExpenseSplit
                    {
                        ExpenseTransactionId = expense.Id,
                        BeneficiaryMemberId = included[i].MemberId,
                        SplitType = SplitType.Equal,
                        SplitAmount = splitAmt
                    });
                }
            }
            else
            {
                foreach (var splitRow in included)
                {
                    _dbContext.ExpenseSplits.Add(new ExpenseSplit
                    {
                        ExpenseTransactionId = expense.Id,
                        BeneficiaryMemberId = splitRow.MemberId,
                        SplitType = SplitType.FixedAmount,
                        SplitAmount = splitRow.AllocatedAmount
                    });
                }
            }
            await _dbContext.SaveChangesAsync();
        }

        // Audit Log
        _dbContext.AuditLogs.Add(new AuditLog
        {
            FamilyId = familyId,
            UserId = userId,
            ActionType = "CREATE",
            EntityName = "ExpenseTransaction",
            EntityId = expense.Id,
            Details = $"Added expense '{expense.Title}' for {expense.Amount:N2}"
        });
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = requiresApproval
            ? "Expense submitted and is awaiting approval by a family admin."
            : $"Expense of {_familyContext.CurrentFamily?.CurrencySymbol}{expense.Amount:N2} recorded successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var expense = await _dbContext.ExpenseTransactions
            .Include(e => e.Category)
            .Include(e => e.PaidByMember).ThenInclude(m => m!.User)
            .Include(e => e.CreatedByUser)
            .Include(e => e.Splits).ThenInclude(s => s.BeneficiaryMember).ThenInclude(m => m!.User)
            .FirstOrDefaultAsync(e => e.Id == id && e.FamilyId == familyId);

        if (expense == null) return NotFound();

        var isOwnerOrAdmin = _familyContext.CurrentRole == FamilyRole.Owner || _familyContext.CurrentRole == FamilyRole.Admin;
        var isCreator = expense.CreatedByUserId == _familyContext.UserId;

        var model = new ExpenseDetailsViewModel
        {
            Expense = expense,
            CurrencySymbol = _familyContext.CurrentFamily?.CurrencySymbol ?? "₹",
            CanEdit = isOwnerOrAdmin || isCreator,
            CanDelete = isOwnerOrAdmin || isCreator,
            CanApprove = isOwnerOrAdmin && expense.NeedsApproval && !expense.IsApproved
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var expense = await _dbContext.ExpenseTransactions.FirstOrDefaultAsync(e => e.Id == id && e.FamilyId == familyId);

        if (expense == null) return NotFound();

        var isOwnerOrAdmin = _familyContext.CurrentRole == FamilyRole.Owner || _familyContext.CurrentRole == FamilyRole.Admin;
        if (!isOwnerOrAdmin && expense.CreatedByUserId != _familyContext.UserId)
        {
            TempData["ErrorMessage"] = "You do not have permission to delete this expense.";
            return RedirectToAction(nameof(Index));
        }

        if (!string.IsNullOrWhiteSpace(expense.ReceiptPath))
        {
            await _fileStorage.DeleteFileAsync(expense.ReceiptPath);
        }

        _dbContext.ExpenseTransactions.Remove(expense);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            FamilyId = familyId,
            UserId = _familyContext.UserId,
            ActionType = "DELETE",
            EntityName = "ExpenseTransaction",
            EntityId = id,
            Details = $"Deleted expense '{expense.Title}' for {expense.Amount:N2}"
        });

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Expense deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> PendingApprovals()
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var pending = await _dbContext.ExpenseTransactions
            .Include(e => e.Category)
            .Include(e => e.PaidByMember).ThenInclude(m => m!.User)
            .Include(e => e.CreatedByUser)
            .Where(e => e.FamilyId == familyId && e.NeedsApproval && !e.IsApproved)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return View(pending);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(string id)
    {
        if (_familyContext.CurrentRole != FamilyRole.Owner && _familyContext.CurrentRole != FamilyRole.Admin)
        {
            TempData["ErrorMessage"] = "Only family owners or admins can approve expenses.";
            return RedirectToAction(nameof(Index));
        }

        var familyId = _familyContext.CurrentFamilyId!;
        var expense = await _dbContext.ExpenseTransactions.FirstOrDefaultAsync(e => e.Id == id && e.FamilyId == familyId);

        if (expense == null) return NotFound();

        expense.IsApproved = true;
        expense.Status = TransactionStatus.Confirmed;
        expense.ApprovedByUserId = _familyContext.UserId;
        expense.UpdatedAt = DateTime.UtcNow;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            FamilyId = familyId,
            UserId = _familyContext.UserId,
            ActionType = "APPROVE",
            EntityName = "ExpenseTransaction",
            EntityId = id,
            Details = $"Approved expense '{expense.Title}' of {expense.Amount:N2}"
        });

        await _dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Expense '{expense.Title}' approved.";
        return RedirectToAction(nameof(PendingApprovals));
    }

    private async Task<List<FinancialCategory>> GetExpenseCategoriesAsync(string familyId)
    {
        return await _dbContext.FinancialCategories
            .Where(c => c.IsSystemDefault || c.FamilyId == familyId)
            .Where(c => c.Type == CategoryType.Expense && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    private async Task<List<FamilyMembership>> GetFamilyMembersAsync(string familyId)
    {
        return await _dbContext.FamilyMemberships
            .Include(m => m.User)
            .Where(m => m.FamilyId == familyId && m.IsActive)
            .ToListAsync();
    }
}
