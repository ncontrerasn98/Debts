namespace Debts.Application.Abstractions.Messaging;

public interface IEventProducer
{
    Task PublishAsync<T>(string topic, T message);
}