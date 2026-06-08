namespace Debts.Application.Abstractions.Messaging;

public interface IRabbitMqPublisher
{
    Task PublishToTopicAsync<T>(
        string routingKey,
        T message,
        CancellationToken cancellationToken = default);

    Task PublishToFanoutAsync<T>(
        T message,
        CancellationToken cancellationToken = default);
}