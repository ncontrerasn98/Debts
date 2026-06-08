using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Notification.Service.Data;
using Notification.Service.Hubs;
using Notification.Service.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;

namespace Notification.Service.Consumers;

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

        // Topología — idempotente
        await _channel.ExchangeDeclareAsync(
            exchange: "debts.topic",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: "notification.debt.created",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: "notification.debt.created",
            exchange: "debts.topic",
            routingKey: "debt.created.#",
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
                    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
                    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

                    var preferences = await dbContext.NotificationPreferences
                        .FirstOrDefaultAsync(p => p.UserId == debtCreatedEvent!.UserId, stoppingToken);

                    var emailEnabled = preferences?.EmailEnabled ?? true;
                    var signalREnabled = preferences?.SignalREnabled ?? true;

                    if (emailEnabled)
                    {
                        await emailSender.SendAsync(
                            to: "test-mail@email.com",
                            subject: "New debt created",
                            body: $"""
                                <h2>Debt Created</h2>
                                <p>A new debt <strong>{debtCreatedEvent!.DebtId}</strong>
                                for <strong>${debtCreatedEvent.Amount}</strong>
                                was created on {debtCreatedEvent.CreatedAt:dd MMM yyyy HH:mm} UTC.</p>
                                """);

                        dbContext.Notifications.Add(new Entities.Notification(
                            debtCreatedEvent!.UserId,
                            Entities.Notification.Types.DebtCreated,
                            Entities.Notification.Channels.Email,
                            $"Debt {debtCreatedEvent.DebtId} created"));
                    }

                    if (signalREnabled)
                    {
                        await hubContext.Clients
                            .Group($"user-{debtCreatedEvent!.UserId}")
                            .SendAsync("DebtCreated", new
                            {
                                debtCreatedEvent!.DebtId,
                                debtCreatedEvent.Amount,
                                debtCreatedEvent.CreatedAt
                            }, stoppingToken);

                        dbContext.Notifications.Add(new Entities.Notification(
                            debtCreatedEvent!.UserId,
                            Entities.Notification.Types.DebtCreated,
                            Entities.Notification.Channels.SignalR,
                            $"Debt {debtCreatedEvent.DebtId} created"));
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation(
                        "Notifications sent for DebtCreated — debt {DebtId}, user {UserId}",
                        debtCreatedEvent!.DebtId,
                        debtCreatedEvent.UserId);
                }

                if (ea.RoutingKey.StartsWith("debt.compensated"))
                {
                    var compensatedEvent = JsonSerializer.Deserialize<DebtCompensatedEvent>(body);

                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

                    // Notificar al usuario que su deuda fue cancelada
                    await hubContext.Clients
                        .Group($"user-{compensatedEvent!.UserId}")
                        .SendAsync("DebtCompensated", new
                        {
                            compensatedEvent!.DebtId,
                            compensatedEvent.Reason
                        }, stoppingToken);

                    dbContext.Notifications.Add(new Entities.Notification(
                        compensatedEvent!.UserId,
                        Entities.Notification.Types.DebtCompensated,
                        Entities.Notification.Channels.SignalR,
                        $"Debt {compensatedEvent.DebtId} creation cancelled — {compensatedEvent.Reason}"));

                    await dbContext.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation(
                        "Compensation notification sent for debt {DebtId}",
                        compensatedEvent!.DebtId);
                }

                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing message with routing key {RoutingKey}",
                    ea.RoutingKey);

                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: "notification.debt.created",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

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