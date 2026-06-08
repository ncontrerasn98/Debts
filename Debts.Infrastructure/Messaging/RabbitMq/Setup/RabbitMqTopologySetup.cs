using Debts.Infrastructure.Messaging.RabbitMq.Constants;
using RabbitMQ.Client;

namespace Debts.Infrastructure.Messaging.RabbitMq.Setup;

public static class RabbitMqTopologySetup
{
    public static async Task InitializeAsync(IConnection connection)
    {
        await using var channel = await connection.CreateChannelAsync();

        // Exchanges
        await channel.ExchangeDeclareAsync(
            exchange: RabbitMqConstants.Exchanges.Topic,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        await channel.ExchangeDeclareAsync(
            exchange: RabbitMqConstants.Exchanges.Fanout,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false);

        // Queues Topic
        await channel.QueueDeclareAsync(RabbitMqConstants.Queues.CreditScoreDebtEvents,
            durable: true, exclusive: false, autoDelete: false);

        await channel.QueueDeclareAsync(RabbitMqConstants.Queues.NotificationDebtCreated,
            durable: true, exclusive: false, autoDelete: false);
        
        // Queues compensación Topic
        await channel.QueueDeclareAsync(RabbitMqConstants.Queues.CreditScoreDebtCompensated,
            durable: true, exclusive: false, autoDelete: false);

        await channel.QueueDeclareAsync(RabbitMqConstants.Queues.NotificationDebtCompensated,
            durable: true, exclusive: false, autoDelete: false);

        // Bindings compensación — pattern debt.compensated.#
        await channel.QueueBindAsync(RabbitMqConstants.Queues.CreditScoreDebtCompensated,
            RabbitMqConstants.Exchanges.Topic,
            RabbitMqConstants.Patterns.DebtCompensated);

        await channel.QueueBindAsync(RabbitMqConstants.Queues.NotificationDebtCompensated,
            RabbitMqConstants.Exchanges.Topic,
            RabbitMqConstants.Patterns.DebtCompensated);

        // Fanout ya cubre todo — no necesita queues extra

        // Bindings Topic
        await channel.QueueBindAsync(
            RabbitMqConstants.Queues.CreditScoreDebtEvents,
            RabbitMqConstants.Exchanges.Topic,
            RabbitMqConstants.Patterns.AllDebtEvents);       // debt.#

        await channel.QueueBindAsync(
            RabbitMqConstants.Queues.NotificationDebtCreated,
            RabbitMqConstants.Exchanges.Topic,
            RabbitMqConstants.Patterns.DebtCreatedOnly);     // debt.created.#

        // Queues Fanout
        await channel.QueueDeclareAsync(RabbitMqConstants.Queues.CreditScoreFanout,
            durable: true, exclusive: false, autoDelete: false);

        await channel.QueueDeclareAsync(RabbitMqConstants.Queues.NotificationFanout,
            durable: true, exclusive: false, autoDelete: false);

        // Bindings Fanout — routing key vacía, fanout la ignora
        await channel.QueueBindAsync(
            RabbitMqConstants.Queues.CreditScoreFanout,
            RabbitMqConstants.Exchanges.Fanout,
            routingKey: "");

        await channel.QueueBindAsync(
            RabbitMqConstants.Queues.NotificationFanout,
            RabbitMqConstants.Exchanges.Fanout,
            routingKey: "");
    }
}