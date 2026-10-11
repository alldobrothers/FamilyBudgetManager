using FamilyBudget.Domain.Entities;
using FamilyBudget.Domain.Enums;

namespace FamilyBudget.Application.Interfaces;

public interface ICurrentFamilyContext
{
    string? CurrentFamilyId { get; }
    Family? CurrentFamily { get; }
    FamilyMembership? CurrentMembership { get; }
    FamilyRole? CurrentRole { get; }
    bool IsAuthenticated { get; }
    string? UserId { get; }
    Task<bool> SetActiveFamilyAsync(string familyId);
    bool HasPermission(Func<FamilyMembership, bool> permissionCheck);
}
