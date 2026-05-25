using Debts.Domain.Entities;

namespace Debts.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task AddAsync(User user);
    Task<bool> ExistsAsync(Guid id);
    Task<User?> GetByNameAsync(string name);
}