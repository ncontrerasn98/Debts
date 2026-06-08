using Debts.Application.Abstractions.Persistence;
using Debts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Debts.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct)
    {
        return _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == token);
    }

    public Task AddAsync(RefreshToken token, CancellationToken ct)
    {
        _context.RefreshTokens.Add(token);
        return Task.CompletedTask;
    }
    
    public async Task<IEnumerable<RefreshToken>> GetFamilyAsync(Guid familyId, CancellationToken ct)
    {
        return await _context.RefreshTokens
            .Where(x => x.FamilyId == familyId)
            .ToListAsync(ct);
    }

    public async Task RevokeAllFamilyAsync(Guid familyId, CancellationToken ct)
    {
        var family = await _context.RefreshTokens
            .Where(x => x.FamilyId == familyId)
            .ToListAsync(ct);

        foreach (var token in family)
        {
            token.IsCompromised = true;
            token.RevokedAt ??= DateTime.UtcNow;  // solo si no estaba ya revocado
        }
    }
    
}