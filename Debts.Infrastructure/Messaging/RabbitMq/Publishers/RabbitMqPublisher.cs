using System.Text;
using System.Text.Json;
using Debts.Application.Abstractions.Messaging;
using Debts.Infrastructure.Messaging.RabbitMq.Constants;
using RabbitMQ.Client;

namespace Debts.Infrastructure.Messaging.RabbitMq.Publishers;

public class RabbitMqPublisher : IRabbitMqPublisher, IAsyncDisposable
{
    private readonly IChannel _channel;

    public RabbitMqPublisher(IConnection connection)
    {
        _channel = connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public async Task PublishToTopicAsync<T>(
        string routingKey,
        T message,
        CancellationToken cancellationToken = default)
    {
        var body = Serialize(message);

        await _channel.BasicPublishAsync(
            exchange: RabbitMqConstants.Exchanges.Topic,
            routingKey: routingKey,
            body: body,
            cancellationToken: cancellationToken);
    }

    public async Task PublishToFanoutAsync<T>(
        T message,
        CancellationToken cancellationToken = default)
    {
        var body = Serialize(message);

        await _channel.BasicPublishAsync(
            exchange: RabbitMqConstants.Exchanges.Fanout,
            routingKey: "",         // fanout la ignora
            body: body,
            cancellationToken: cancellationToken);
    }

    private static ReadOnlyMemory<byte> Serialize<T>(T message)
    {
        var json = JsonSerializer.Serialize(message);
        return Encoding.UTF8.GetBytes(json);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        _channel.Dispose();
    }
}