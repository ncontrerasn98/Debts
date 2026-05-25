using Debts.Application.Abstractions.Persistence;
using Debts.Application.Abstractions.Webhooks;
using Debts.Domain.Exceptions;
using MediatR;

namespace Debts.Application.Commands.Webhooks.Deactivate;

public class DeactivateWebhookHandler : IRequestHandler<DeactivateWebhookCommand>
{
    private readonly IWebhookRepository _webhookRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateWebhookHandler(
        IWebhookRepository webhookRepository,
        IUnitOfWork unitOfWork)
    {
        _webhookRepository = webhookRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateWebhookCommand command, CancellationToken cancellationToken)
    {
        var subscription = await _webhookRepository.GetByIdAsync(command.WebhookId);

        if (subscription is null)
            throw new NotFoundException("Webhook subscription not found");

        subscription.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}