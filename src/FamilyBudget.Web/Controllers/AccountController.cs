using FamilyBudget.Domain.Entities;
using FamilyBudget.Domain.Enums;
using FamilyBudget.Infrastructure.Data;
using FamilyBudget.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly FamilyBudgetDbContext _dbContext;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        FamilyBudgetDbContext dbContext,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in successfully.", model.Email);
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        ModelState.AddModelError(string.Empty, "Invalid login credentials. Please verify your email and password.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            DisplayName = model.FullName.Split(' ').FirstOrDefault() ?? model.FullName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} registered successfully.", model.Email);

            // Create initial family if specified, or default
            var familyName = !string.IsNullOrWhiteSpace(model.InitialFamilyName)
                ? model.InitialFamilyName
                : $"{user.DisplayName}'s Household";

            var family = new Family
            {
                Name = familyName,
                CurrencyCode = "INR",
                CurrencySymbol = "₹",
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Families.Add(family);
            await _dbContext.SaveChangesAsync();

            // Family Settings
            _dbContext.FamilySettings.Add(new FamilySettings
            {
                FamilyId = family.Id
            });

            // Owner Membership
            _dbContext.FamilyMemberships.Add(new FamilyMembership
            {
                FamilyId = family.Id,
                UserId = user.Id,
                Role = FamilyRole.Owner,
                JoinedAt = DateTime.UtcNow,
                CanApproveExpenses = true,
                CanManageBills = true,
                CanViewDebts = true,
                CanExportReports = true
            });

            // User preference
            _dbContext.UserPreferences.Add(new UserPreference
            {
                UserId = user.Id,
                DefaultFamilyId = family.Id,
                PreferredCurrency = "INR",
                PreferredTheme = "light"
            });

            await _dbContext.SaveChangesAsync();

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        HttpContext.Session.Clear();
        Response.Cookies.Delete("ActiveFamilyId");
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var pref = await _dbContext.UserPreferences.FirstOrDefaultAsync(p => p.UserId == user.Id);

        var model = new UserProfileViewModel
        {
            FullName = user.FullName,
            DisplayName = user.DisplayName,
            Email = user.Email ?? string.Empty,
            PreferredTheme = pref?.PreferredTheme ?? "light",
            PreferredCurrency = pref?.PreferredCurrency ?? "INR"
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UserProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        user.FullName = model.FullName;
        user.DisplayName = model.DisplayName;
        await _userManager.UpdateAsync(user);

        var pref = await _dbContext.UserPreferences.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (pref == null)
        {
            pref = new UserPreference { UserId = user.Id };
            _dbContext.UserPreferences.Add(pref);
        }

        pref.PreferredTheme = model.PreferredTheme;
        pref.PreferredCurrency = model.PreferredCurrency;
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Profile settings saved successfully.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
