using Debts.Application.Abstractions.Persistence;
using Debts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Debts.Infrastructure.Persistence.Repositories;

public class OutboxMessagesRepository : IOutboxMessagesRepository
{
    private readonly AppDbContext _context;

    public OutboxMessagesRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<OutboxMessage?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return _context.OutboxMessages
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task AddAsync(OutboxMessage outboxMessage, CancellationToken ct)
    {
        _context.OutboxMessages.Add(outboxMessage);
        return Task.CompletedTask;
    }

    public async Task DeletePendingAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        var pending = await _context.OutboxMessages
            .Where(x => x.CorrelationId == correlationId && x.ProcessedOnUtc == null)
            .ToListAsync(cancellationToken);

        _context.OutboxMessages.RemoveRange(pending);
    }
}