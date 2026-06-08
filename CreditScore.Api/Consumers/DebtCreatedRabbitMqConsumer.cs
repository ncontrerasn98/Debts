using System.Text;
using System.Text.Json;
using CreditScore.Api.Data;
using CreditScore.Api.Entities;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;

namespace CreditScore.Api.Consumers;

public class DebtCreatedRabbitMqConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DebtCreatedRabbitMqConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;

    public DebtCreatedRabbitMqConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<DebtCreatedRabbitMqConsumer> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"],
            UserName = _configuration["RabbitMQ:Username"],
            Password = _configuration["RabbitMQ:Password"]
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);

        // Declarar exchange y queue — idempotente, seguro correrlo siempre
        await _channel.ExchangeDeclareAsync(
            exchange: "debts.topic",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: "creditscore.debt.events",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: "creditscore.debt.events",
            exchange: "debts.topic",
            routingKey: "debt.#",
            cancellationToken: cancellationToken);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel!);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                if (ea.RoutingKey.StartsWith("debt.created"))
                {
                    var debtCreatedEvent = JsonSerializer.Deserialize<DebtCreatedEvent>(body);

                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<CreditScoreDbContext>();

                    var history = await dbContext.CreditHistories
                        .Include(h => h.Events)   // ← cargar eventos para idempotencia
                        .FirstOrDefaultAsync(h => h.UserId == debtCreatedEvent!.UserId, stoppingToken);

                    if (history is null)
                    {
                        history = new CreditHistory(debtCreatedEvent!.UserId);
                        dbContext.CreditHistories.Add(history);
                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("CreditHistory created with Id {Id}", history.Id);
                    }
                    else
                    {
                        _logger.LogInformation("CreditHistory found with Id {Id}, Events count: {Count}", 
                            history.Id, history.Events.Count);
                    }

                    var applied = history.ApplyDebtCreated(debtCreatedEvent!.DebtId, debtCreatedEvent.Amount);

                    if (!applied)
                    {
                        _logger.LogInformation(
                            "DebtCreatedEvent {DebtId} already processed — skipping",
                            debtCreatedEvent!.DebtId);

                        await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                        return;
                    }

                    // Agregar el último evento explícitamente al contexto
                    var newEvent = history.Events.Last();
                    dbContext.Entry(newEvent).State = EntityState.Added;  // ← forzar INSERT

                    await dbContext.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation(
                        "CreditHistory updated for DebtCreated — user {UserId}, debt {DebtId}",
                        debtCreatedEvent.UserId,
                        debtCreatedEvent.DebtId);
                }

                // Manejar DebtCompensatedEvent
                if (ea.RoutingKey.StartsWith("debt.compensated"))
                {
                    var compensatedEvent = JsonSerializer.Deserialize<DebtCompensatedEvent>(body);

                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<CreditScoreDbContext>();

                    var history = await dbContext.CreditHistories
                        .Include(h => h.Events)   // ← cargar eventos para revert
                        .FirstOrDefaultAsync(h => h.UserId == compensatedEvent!.UserId, stoppingToken);

                    if (history is null)
                    {
                        _logger.LogInformation(
                            "No CreditHistory found for user {UserId} — compensation ignored",
                            compensatedEvent!.UserId);

                        await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                        return;
                    }

                    var reverted = history.RevertDebtCreated(compensatedEvent!.DebtId);

                    if (!reverted)
                    {
                        _logger.LogInformation(
                            "DebtCreatedEvent {DebtId} was never processed — compensation ignored",
                            compensatedEvent!.DebtId);

                        await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                        return;
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation(
                        "CreditHistory reverted for DebtCompensated — user {UserId}, debt {DebtId}",
                        compensatedEvent!.UserId,
                        compensatedEvent.DebtId);
                }

                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing message with routing key {RoutingKey}",
                    ea.RoutingKey);

                // Nack sin requeue — va a DLQ si está configurada, se descarta si no
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: "creditscore.debt.events",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        // Mantener el consumer vivo hasta que se canceleß
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel is not null)
            await _channel.CloseAsync(cancellationToken);

        if (_connection is not null)
            await _connection.CloseAsync(cancellationToken);
    }
}