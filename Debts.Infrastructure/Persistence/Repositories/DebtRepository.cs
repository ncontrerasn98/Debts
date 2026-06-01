using Debts.Application.Abstractions.Persistence;
using Debts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Debts.Infrastructure.Persistence.Repositories;

public class DebtRepository : IDebtRepository
{
    private readonly AppDbContext _context;

    public DebtRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Debt debt)
    {
        await _context.Debts.AddAsync(debt);
    }
    
    public async Task<List<Debt>> GetAllAsync()
    {
        return await _context.Debts
            .AsNoTracking()
            .ToListAsync();
    }
    
    public async Task<Debt?> GetByIdAsync(Guid id)
    {
        return await _context.Debts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }
    
    public async Task<Debt?> GetTrackedByIdAsync(Guid id)
    {
        return await _context.Debts
            .FirstOrDefaultAsync(x => x.Id == id);
    }
    
    public void Remove(Debt debt)
        => _context.Debts.Remove(debt);

}