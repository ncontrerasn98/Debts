using Debts.Domain.Entities;

namespace Debts.Application.Abstractions.Persistence;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name);
    Task<bool> UserHasRoleAsync(Guid userId, Guid roleId);
    Task AssignRoleAsync(UserRole userRole);
    Task RevokeRoleAsync(Guid userId, Guid roleId);
}