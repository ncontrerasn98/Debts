using Debts.Application.Abstractions.Webhooks;
using Debts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Debts.Infrastructure.Persistence.Repositories;

public class WebhookRepository : IWebhookRepository
{
    private readonly AppDbContext _context;

    public WebhookRepository(AppDbContext context)
        => _context = context;

    public async Task<WebhookSubscription?> GetByIdAsync(Guid id)
        => await _context.WebhookSubscriptions
            .FirstOrDefaultAsync(w => w.Id == id);

    public async Task AddAsync(WebhookSubscription subscription)
        => await _context.WebhookSubscriptions.AddAsync(subscription);
}