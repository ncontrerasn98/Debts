using Debts.Application.Abstractions.Persistence;
using Debts.Application.Abstractions.Webhooks;
using Debts.Domain.Entities;
using MediatR;

namespace Debts.Application.Commands.Webhooks.Register;

public class RegisterWebhookHandler : IRequestHandler<RegisterWebhookCommand, Guid>
{
    private readonly IWebhookRepository _webhookRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterWebhookHandler(
        IWebhookRepository webhookRepository,
        IUnitOfWork unitOfWork)
    {
        _webhookRepository = webhookRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(RegisterWebhookCommand command, CancellationToken cancellationToken)
    {
        var subscription = new WebhookSubscription(command.Url, command.EventType);

        await _webhookRepository.AddAsync(subscription);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return subscription.Id;
    }
}