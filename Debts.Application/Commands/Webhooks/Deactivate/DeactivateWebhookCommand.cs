using MediatR;

namespace Debts.Application.Commands.Webhooks.Deactivate;

public class DeactivateWebhookCommand : IRequest
{
    public Guid WebhookId { get; set; }
}