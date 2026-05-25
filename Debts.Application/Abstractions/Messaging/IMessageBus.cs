namespace Debts.Application.Abstractions.Messaging;

public interface IMessageBus
{
    Task PublishAsync<T>(
        T message,
        CancellationToken cancellationToken = default)
        where T : class;
}