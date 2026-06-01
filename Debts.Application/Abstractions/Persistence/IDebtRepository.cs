using Debts.Domain.Entities;

namespace Debts.Application.Abstractions.Persistence;

public interface IDebtRepository
{
    Task AddAsync(Debt debt);
    Task<List<Debt>> GetAllAsync();
    Task<Debt?> GetByIdAsync(Guid id);
    Task<Debt?> GetTrackedByIdAsync(Guid id);
    void Remove(Debt debt);
}
