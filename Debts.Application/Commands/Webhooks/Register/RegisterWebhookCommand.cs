using MediatR;

namespace Debts.Application.Commands.Webhooks.Register;

public class RegisterWebhookCommand : IRequest<Guid>
{
    public string Url { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
}