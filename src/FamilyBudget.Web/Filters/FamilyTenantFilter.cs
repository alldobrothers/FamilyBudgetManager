using FamilyBudget.Application.Interfaces;
using FamilyBudget.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Web.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireFamilyAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var familyContext = context.HttpContext.RequestServices.GetRequiredService<ICurrentFamilyContext>();

        if (familyContext.IsAuthenticated)
        {
            if (string.IsNullOrWhiteSpace(familyContext.CurrentFamilyId))
            {
                // User has no family yet -> redirect to Onboarding
                context.Result = new RedirectToActionResult("Onboarding", "Family", null);
                return;
            }

            // Populate ViewData for shared Layout, navigation, and badges
            if (context.Controller is Controller controller)
            {
                controller.ViewBag.CurrentFamily = familyContext.CurrentFamily;
                controller.ViewBag.CurrentMembership = familyContext.CurrentMembership;
                controller.ViewBag.CurrentRole = familyContext.CurrentRole;
                controller.ViewBag.CurrencySymbol = familyContext.CurrentFamily?.CurrencySymbol ?? "₹";
                controller.ViewBag.CurrencyCode = familyContext.CurrentFamily?.CurrencyCode ?? "INR";

                // Load user's all families for the switcher dropdown
                var dbContext = context.HttpContext.RequestServices.GetRequiredService<FamilyBudgetDbContext>();
                var allMemberships = await dbContext.FamilyMemberships
                    .Include(m => m.Family)
                    .Where(m => m.UserId == familyContext.UserId && m.IsActive)
                    .ToListAsync();

                controller.ViewBag.UserFamilies = allMemberships.Select(m => m.Family).Where(f => f != null).ToList();

                // Unread notifications count
                var unreadCount = await dbContext.InAppNotifications
                    .CountAsync(n => n.UserId == familyContext.UserId && !n.IsRead);
                controller.ViewBag.UnreadNotificationCount = unreadCount;
            }
        }

        await next();
    }
}
