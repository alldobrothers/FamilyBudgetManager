using System.Security.Cryptography;
using System.Text;
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
public class FamilyController : Controller
{
    private readonly FamilyBudgetDbContext _dbContext;
    private readonly ICurrentFamilyContext _familyContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<FamilyController> _logger;

    public FamilyController(
        FamilyBudgetDbContext dbContext,
        ICurrentFamilyContext familyContext,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<FamilyController> logger)
    {
        _dbContext = dbContext;
        _familyContext = familyContext;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Onboarding()
    {
        // If user already has a family, redirect to Dashboard
        if (!string.IsNullOrWhiteSpace(_familyContext.CurrentFamilyId))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var userId = _userManager.GetUserId(User);
        var existingMembership = await _dbContext.FamilyMemberships
            .Include(m => m.Family)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.IsActive);

        if (existingMembership != null)
        {
            await _familyContext.SetActiveFamilyAsync(existingMembership.FamilyId);
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new CreateFamilyViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateFamilyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Onboarding", model);
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();

        var family = new Family
        {
            Name = model.Name.Trim(),
            CurrencyCode = model.CurrencyCode.Trim().ToUpperInvariant(),
            CurrencySymbol = model.CurrencySymbol.Trim(),
            MonthlyBudget = model.MonthlyBudget,
            Description = model.Description?.Trim(),
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Families.Add(family);
        await _dbContext.SaveChangesAsync();

        // Settings
        _dbContext.FamilySettings.Add(new FamilySettings
        {
            FamilyId = family.Id
        });

        // Owner Membership
        _dbContext.FamilyMemberships.Add(new FamilyMembership
        {
            FamilyId = family.Id,
            UserId = userId,
            Role = FamilyRole.Owner,
            JoinedAt = DateTime.UtcNow,
            CanApproveExpenses = true,
            CanManageBills = true,
            CanViewDebts = true,
            CanExportReports = true
        });

        // User preference
        var pref = await _dbContext.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        if (pref == null)
        {
            _dbContext.UserPreferences.Add(new UserPreference { UserId = userId, DefaultFamilyId = family.Id });
        }
        else
        {
            pref.DefaultFamilyId = family.Id;
        }

        await _dbContext.SaveChangesAsync();

        await _familyContext.SetActiveFamilyAsync(family.Id);
        TempData["SuccessMessage"] = $"Household '{family.Name}' created successfully!";
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public async Task<IActionResult> Switch(string familyId, string? returnUrl = null)
    {
        var success = await _familyContext.SetActiveFamilyAsync(familyId);
        if (!success)
        {
            TempData["ErrorMessage"] = "You do not have access to that family household.";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    [RequireFamily]
    public async Task<IActionResult> Members()
    {
        var familyId = _familyContext.CurrentFamilyId!;
        var family = await _dbContext.Families.FindAsync(familyId);
        if (family == null) return NotFound();

        var members = await _dbContext.FamilyMemberships
            .Include(m => m.User)
            .Where(m => m.FamilyId == familyId && m.IsActive)
            .OrderBy(m => m.Role)
            .ToListAsync();

        var pendingInvites = await _dbContext.FamilyInvitations
            .Where(i => i.FamilyId == familyId && i.Status == InvitationStatus.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        var model = new FamilyMembersViewModel
        {
            Family = family,
            Members = members,
            PendingInvitations = pendingInvites,
            UserRole = _familyContext.CurrentRole ?? FamilyRole.Member
        };

        return View(model);
    }

    [HttpGet]
    [RequireFamily]
    public IActionResult Invite()
    {
        if (_familyContext.CurrentRole != FamilyRole.Owner && _familyContext.CurrentRole != FamilyRole.Admin)
        {
            TempData["ErrorMessage"] = "Only family Owners or Admins can invite new members.";
            return RedirectToAction(nameof(Members));
        }

        return View(new InviteMemberViewModel
        {
            FamilyId = _familyContext.CurrentFamilyId!,
            FamilyName = _familyContext.CurrentFamily!.Name
        });
    }

    [HttpPost]
    [RequireFamily]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(InviteMemberViewModel model)
    {
        if (_familyContext.CurrentRole != FamilyRole.Owner && _familyContext.CurrentRole != FamilyRole.Admin)
        {
            TempData["ErrorMessage"] = "Only family Owners or Admins can invite new members.";
            return RedirectToAction(nameof(Members));
        }

        if (!ModelState.IsValid)
        {
            model.FamilyName = _familyContext.CurrentFamily!.Name;
            return View(model);
        }

        var familyId = _familyContext.CurrentFamilyId!;
        var email = model.Email.Trim().ToLowerInvariant();
        var currentUserId = _userManager.GetUserId(User)!;

        // Check if user is already a member
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            var isMember = await _dbContext.FamilyMemberships
                .AnyAsync(m => m.FamilyId == familyId && m.UserId == existingUser.Id && m.IsActive);
            if (isMember)
            {
                ModelState.AddModelError("Email", "This person is already a member of this household.");
                model.FamilyName = _familyContext.CurrentFamily!.Name;
                return View(model);
            }
        }

        // Generate invitation token
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");
        var tokenHash = ComputeSha256Hash(rawToken);

        var invitation = new FamilyInvitation
        {
            FamilyId = familyId,
            InvitedEmail = email,
            InvitedRole = model.Role,
            TokenHash = tokenHash,
            InvitedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            Status = InvitationStatus.Pending
        };

        _dbContext.FamilyInvitations.Add(invitation);
        await _dbContext.SaveChangesAsync();

        var inviteUrl = Url.Action("AcceptInvite", "Family", new { token = rawToken }, Request.Scheme) 
            ?? $"/Family/AcceptInvite?token={rawToken}";
        var inviterUser = await _userManager.GetUserAsync(User);
        var inviterName = inviterUser?.DisplayName ?? inviterUser?.FullName ?? "A family member";

        await _emailService.SendInvitationEmailAsync(email, _familyContext.CurrentFamily!.Name, inviteUrl, inviterName);

        TempData["SuccessMessage"] = $"Invitation generated for {email}. Share link: {inviteUrl}";
        return RedirectToAction(nameof(Members));
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvite(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return View(new AcceptInviteViewModel { IsExpired = true });
        }

        var tokenHash = ComputeSha256Hash(token);
        var invite = await _dbContext.FamilyInvitations
            .Include(i => i.Family)
            .Include(i => i.InvitedByUser)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash);

        if (invite == null || invite.Status != InvitationStatus.Pending || invite.ExpiresAt < DateTime.UtcNow)
        {
            return View(new AcceptInviteViewModel
            {
                Token = token,
                IsExpired = true
            });
        }

        var model = new AcceptInviteViewModel
        {
            Token = token,
            FamilyName = invite.Family?.Name ?? "Family Household",
            InvitedEmail = invite.InvitedEmail,
            Role = invite.InvitedRole,
            InviterName = invite.InvitedByUser?.DisplayName ?? invite.InvitedByUser?.FullName ?? "A family member"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmAcceptInvite(string token)
    {
        if (!User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("AcceptInvite", "Family", new { token }) });
        }

        var tokenHash = ComputeSha256Hash(token);
        var invite = await _dbContext.FamilyInvitations
            .Include(i => i.Family)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash && i.Status == InvitationStatus.Pending);

        if (invite == null || invite.ExpiresAt < DateTime.UtcNow)
        {
            TempData["ErrorMessage"] = "This invitation has expired or has already been used.";
            return RedirectToAction("Index", "Dashboard");
        }

        var currentUserId = _userManager.GetUserId(User)!;

        // Check if already member
        var existing = await _dbContext.FamilyMemberships
            .FirstOrDefaultAsync(m => m.FamilyId == invite.FamilyId && m.UserId == currentUserId);

        if (existing != null)
        {
            existing.IsActive = true;
            existing.Role = invite.InvitedRole;
        }
        else
        {
            _dbContext.FamilyMemberships.Add(new FamilyMembership
            {
                FamilyId = invite.FamilyId,
                UserId = currentUserId,
                Role = invite.InvitedRole,
                JoinedAt = DateTime.UtcNow,
                CanApproveExpenses = invite.InvitedRole == FamilyRole.Owner || invite.InvitedRole == FamilyRole.Admin,
                CanManageBills = true,
                CanViewDebts = true,
                CanExportReports = true
            });
        }

        invite.Status = InvitationStatus.Accepted;
        invite.AcceptedByUserId = currentUserId;
        invite.AcceptedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        await _familyContext.SetActiveFamilyAsync(invite.FamilyId);
        TempData["SuccessMessage"] = $"Welcome! You have successfully joined {invite.Family?.Name}.";
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    [RequireFamily]
    public async Task<IActionResult> Settings()
    {
        if (_familyContext.CurrentRole != FamilyRole.Owner && _familyContext.CurrentRole != FamilyRole.Admin)
        {
            TempData["ErrorMessage"] = "Only family Owners or Admins can access household settings.";
            return RedirectToAction("Index", "Dashboard");
        }

        var family = _familyContext.CurrentFamily!;
        var settings = await _dbContext.FamilySettings.FirstOrDefaultAsync(s => s.FamilyId == family.Id);

        var model = new FamilySettingsViewModel
        {
            FamilyId = family.Id,
            Name = family.Name,
            CurrencySymbol = family.CurrencySymbol,
            CurrencyCode = family.CurrencyCode,
            MonthStartDay = family.MonthStartDay,
            EnableExpenseApprovals = settings?.EnableExpenseApprovals ?? false,
            BudgetWarningThreshold1 = settings?.BudgetWarningThreshold1 ?? 75.0m,
            BudgetWarningThreshold2 = settings?.BudgetWarningThreshold2 ?? 90.0m,
            NotifyOnOverdueBills = settings?.NotifyOnOverdueBills ?? true
        };

        return View(model);
    }

    [HttpPost]
    [RequireFamily]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(FamilySettingsViewModel model)
    {
        if (_familyContext.CurrentRole != FamilyRole.Owner && _familyContext.CurrentRole != FamilyRole.Admin)
        {
            TempData["ErrorMessage"] = "Only family Owners or Admins can modify household settings.";
            return RedirectToAction("Index", "Dashboard");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var family = await _dbContext.Families.FindAsync(_familyContext.CurrentFamilyId);
        if (family == null) return NotFound();

        family.Name = model.Name.Trim();
        family.CurrencySymbol = model.CurrencySymbol.Trim();
        family.CurrencyCode = model.CurrencyCode.Trim().ToUpperInvariant();
        family.MonthStartDay = model.MonthStartDay;

        var settings = await _dbContext.FamilySettings.FirstOrDefaultAsync(s => s.FamilyId == family.Id);
        if (settings == null)
        {
            settings = new FamilySettings { FamilyId = family.Id };
            _dbContext.FamilySettings.Add(settings);
        }

        settings.EnableExpenseApprovals = model.EnableExpenseApprovals;
        settings.BudgetWarningThreshold1 = model.BudgetWarningThreshold1;
        settings.BudgetWarningThreshold2 = model.BudgetWarningThreshold2;
        settings.NotifyOnOverdueBills = model.NotifyOnOverdueBills;
        settings.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Household settings updated successfully.";
        return RedirectToAction(nameof(Settings));
    }

    private static string ComputeSha256Hash(string rawData)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
