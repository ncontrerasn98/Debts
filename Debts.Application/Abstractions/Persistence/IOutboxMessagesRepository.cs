using Debts.Domain.Entities;

namespace Debts.Application.Abstractions.Persistence;

public interface IOutboxMessagesRepository
{
    Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(OutboxMessage outboxMessage, CancellationToken ct);
}