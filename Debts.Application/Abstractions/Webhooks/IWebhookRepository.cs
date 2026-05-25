using Debts.Domain.Entities;

namespace Debts.Application.Abstractions.Webhooks;

public interface IWebhookRepository
{
    Task<WebhookSubscription?> GetByIdAsync(Guid id);
    Task AddAsync(WebhookSubscription subscription);
}