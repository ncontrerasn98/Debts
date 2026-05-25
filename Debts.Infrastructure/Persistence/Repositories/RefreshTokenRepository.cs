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
    
}