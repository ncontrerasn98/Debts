using Debts.Domain.Entities;

namespace Debts.Application.Abstractions.Webhooks;

public interface IWebhookDispatcher
{
    Task DispatchAsync(WebhookDeliveryAttempt attempt, CancellationToken cancellationToken = default);
}