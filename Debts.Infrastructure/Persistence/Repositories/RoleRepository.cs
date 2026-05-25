using Debts.Application.Abstractions.Persistence;
using Debts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Debts.Infrastructure.Persistence.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _context;

    public RoleRepository(AppDbContext context)
        => _context = context;

    public async Task<Role?> GetByNameAsync(string name)
        => await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == name);

    public async Task<bool> UserHasRoleAsync(Guid userId, Guid roleId)
        => await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

    public async Task AssignRoleAsync(UserRole userRole)
        => await _context.UserRoles.AddAsync(userRole);

    public async Task RevokeRoleAsync(Guid userId, Guid roleId)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (userRole is not null)
            _context.UserRoles.Remove(userRole);
    }
}