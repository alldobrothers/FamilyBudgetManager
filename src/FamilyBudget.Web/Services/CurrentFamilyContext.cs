using System.Security.Claims;
using FamilyBudget.Application.Interfaces;
using FamilyBudget.Domain.Entities;
using FamilyBudget.Domain.Enums;
using FamilyBudget.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Web.Services;

public class CurrentFamilyContext : ICurrentFamilyContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly FamilyBudgetDbContext _dbContext;

    private bool _initialized;
    private string? _currentFamilyId;
    private Family? _currentFamily;
    private FamilyMembership? _currentMembership;

    public CurrentFamilyContext(
        IHttpContextAccessor httpContextAccessor,
        FamilyBudgetDbContext dbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    private HttpContext? HttpContext => _httpContextAccessor.HttpContext;

    public bool IsAuthenticated => HttpContext?.User?.Identity?.IsAuthenticated == true;

    public string? UserId => HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? CurrentFamilyId
    {
        get
        {
            EnsureInitialized();
            return _currentFamilyId;
        }
    }

    public Family? CurrentFamily
    {
        get
        {
            EnsureInitialized();
            return _currentFamily;
        }
    }

    public FamilyMembership? CurrentMembership
    {
        get
        {
            EnsureInitialized();
            return _currentMembership;
        }
    }

    public FamilyRole? CurrentRole => CurrentMembership?.Role;

    public async Task<bool> SetActiveFamilyAsync(string familyId)
    {
        if (string.IsNullOrWhiteSpace(familyId) || string.IsNullOrWhiteSpace(UserId))
            return false;

        var membership = await _dbContext.FamilyMemberships
            .Include(m => m.Family)
            .FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == UserId && m.IsActive);

        if (membership == null)
            return false;

        _currentFamilyId = familyId;
        _currentFamily = membership.Family;
        _currentMembership = membership;

        if (HttpContext != null)
        {
            HttpContext.Session.SetString("ActiveFamilyId", familyId);
            HttpContext.Response.Cookies.Append("ActiveFamilyId", familyId, new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        }

        // Persist to UserPreference
        var pref = await _dbContext.UserPreferences.FirstOrDefaultAsync(p => p.UserId == UserId);
        if (pref != null)
        {
            pref.DefaultFamilyId = familyId;
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            _dbContext.UserPreferences.Add(new UserPreference
            {
                UserId = UserId,
                DefaultFamilyId = familyId
            });
            await _dbContext.SaveChangesAsync();
        }

        return true;
    }

    public bool HasPermission(Func<FamilyMembership, bool> permissionCheck)
    {
        if (CurrentMembership == null) return false;
        // Owners always have all permissions
        if (CurrentMembership.Role == FamilyRole.Owner) return true;
        return permissionCheck(CurrentMembership);
    }

    private void EnsureInitialized()
    {
        if (_initialized || !IsAuthenticated || string.IsNullOrWhiteSpace(UserId))
            return;

        _initialized = true;

        // 1. Try Session
        var activeFamilyId = HttpContext?.Session.GetString("ActiveFamilyId");

        // 2. Try Cookie
        if (string.IsNullOrWhiteSpace(activeFamilyId) && HttpContext != null)
        {
            activeFamilyId = HttpContext.Request.Cookies["ActiveFamilyId"];
        }

        // 3. Try User Preference
        if (string.IsNullOrWhiteSpace(activeFamilyId))
        {
            var pref = _dbContext.UserPreferences.AsNoTracking().FirstOrDefault(p => p.UserId == UserId);
            if (!string.IsNullOrWhiteSpace(pref?.DefaultFamilyId))
            {
                activeFamilyId = pref.DefaultFamilyId;
            }
        }

        // 4. Validate or fallback to first active membership
        var memberships = _dbContext.FamilyMemberships
            .Include(m => m.Family)
            .Where(m => m.UserId == UserId && m.IsActive)
            .ToList();

        if (!memberships.Any())
            return;

        FamilyMembership? targetMembership = null;
        if (!string.IsNullOrWhiteSpace(activeFamilyId))
        {
            targetMembership = memberships.FirstOrDefault(m => m.FamilyId == activeFamilyId);
        }

        targetMembership ??= memberships.FirstOrDefault();

        if (targetMembership != null)
        {
            _currentFamilyId = targetMembership.FamilyId;
            _currentFamily = targetMembership.Family;
            _currentMembership = targetMembership;

            HttpContext?.Session.SetString("ActiveFamilyId", targetMembership.FamilyId);
        }
    }
}
