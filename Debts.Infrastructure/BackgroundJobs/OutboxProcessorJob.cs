using System.Diagnostics;
using System.Text.Json;
using Debts.Application.Abstractions.Messaging;
using Debts.Application.Messaging.Commands;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;

namespace Debts.Infrastructure.BackgroundJobs;

public class OutboxProcessorJob: BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorJob> _logger;
    private readonly IEventProducer _eventProducer;
    private static readonly ActivitySource _activitySource = new("OutboxDispatcher");
    private readonly IRabbitMqPublisher _rabbitMqPublisher;


    public OutboxProcessorJob(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorJob> logger, IEventProducer eventProducer, IRabbitMqPublisher rabbitMqPublisher)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _eventProducer = eventProducer;
        _rabbitMqPublisher = rabbitMqPublisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessMessages(stoppingToken);

            await Task.Delay(5000, stoppingToken);
        }
    }

    private async Task ProcessMessages(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        
        var sendEndpointProvider = scope.ServiceProvider.GetRequiredService<ISendEndpointProvider>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var messages = await context.OutboxMessages
            .Where(x => x.ProcessedOnUtc == null)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                switch (message.Type)
                {
                    case nameof(DebtSettledEvent):
                        var debtSettledEvent = JsonSerializer.Deserialize<DebtSettledEvent>(message.Payload);
                        ActivityContext debtSettledContext = default;
                        if (!string.IsNullOrEmpty(message.TraceParent))
                            ActivityContext.TryParse(message.TraceParent, null, isRemote: true, out debtSettledContext);

                        using (var activity = _activitySource.StartActivity(
                                   "outbox.dispatch.DebtSettledEvent",
                                   ActivityKind.Producer,
                                   debtSettledContext))
                        {
                            await _eventProducer.PublishAsync("debt-settled", debtSettledEvent);

                            _logger.LogInformation(
                                "Outbox message {Type} processed",
                                message.Type);
                        }
                        break;
                    
                    case nameof(SendDebtSettledEmailCommand):
                        var command = JsonSerializer.Deserialize<SendDebtSettledEmailCommand>(message.Payload);

                        // Restaurar el ActivityContext desde la outbox
                        ActivityContext parentContext = default;
                        if (!string.IsNullOrEmpty(message.TraceParent))
                            ActivityContext.TryParse(message.TraceParent, null, isRemote: true, out parentContext);

                        using (var activity = _activitySource.StartActivity(
                                   "outbox.dispatch.SendDebtSettledEmail",
                                   ActivityKind.Producer,
                                   parentContext))
                        {
                            var endpoint = await sendEndpointProvider.GetSendEndpoint(
                                new Uri("queue:SendDebtSettledEmail"));

                            // MassTransit propaga el Activity.Current automáticamente
                            // en los headers del mensaje si tiene OTel configurado.
                            // El pipe es solo por si necesitas forzarlo manualmente.
                            await endpoint.Send(command, ctx =>
                            {
                                ctx.Headers.Set("traceparent", activity?.Id);
                                if (!string.IsNullOrEmpty(Activity.Current?.TraceStateString))
                                    ctx.Headers.Set("tracestate", Activity.Current.TraceStateString);
                            }, cancellationToken);
                            
                            _logger.LogInformation(
                                "Outbox message {Type} processed",
                                message.Type);
                        }

                        break;
                    
                    case nameof(DebtCreatedEvent):
                        var debtCreatedTopic = JsonSerializer.Deserialize<DebtCreatedEvent>(message.Payload);

                        ActivityContext debtCreatedContext = default;
                        if (!string.IsNullOrEmpty(message.TraceParent))
                            ActivityContext.TryParse(message.TraceParent, null, isRemote: true, out debtCreatedContext);

                        using (var activity = _activitySource.StartActivity(
                                   "outbox.dispatch.DebtCreatedEvent.Topic",
                                   ActivityKind.Producer,
                                   debtCreatedContext))
                        {
                            var routingKey = $"debt.created.{debtCreatedTopic!.Region}";

                            await _rabbitMqPublisher.PublishToTopicAsync(
                                routingKey: routingKey,
                                message: debtCreatedTopic, cancellationToken: cancellationToken);

                            _logger.LogInformation(
                                "Outbox message {Type} published to Topic Exchange with routing key {RoutingKey}",
                                message.Type,
                                routingKey);
                        }
                        break;

                    case $"{nameof(DebtCreatedEvent)}.Fanout":
                        var debtCreatedFanout = JsonSerializer.Deserialize<DebtCreatedEvent>(message.Payload);

                        ActivityContext debtCreatedFanoutContext = default;
                        if (!string.IsNullOrEmpty(message.TraceParent))
                            ActivityContext.TryParse(message.TraceParent, null, isRemote: true, out debtCreatedFanoutContext);

                        using (var activity = _activitySource.StartActivity(
                                   "outbox.dispatch.DebtCreatedEvent.Fanout",
                                   ActivityKind.Producer,
                                   debtCreatedFanoutContext))
                        {
                            await _rabbitMqPublisher.PublishToFanoutAsync(message: debtCreatedFanout!, cancellationToken: cancellationToken);

                            _logger.LogInformation(
                                "Outbox message {Type} published to Fanout Exchange",
                                message.Type);
                        }
                        break;
                    
                    case nameof(DebtCompensatedEvent):
                        var compensatedTopic = JsonSerializer.Deserialize<DebtCompensatedEvent>(message.Payload);

                        ActivityContext compensatedTopicContext = default;
                        if (!string.IsNullOrEmpty(message.TraceParent))
                            ActivityContext.TryParse(message.TraceParent, null, isRemote: true, out compensatedTopicContext);

                        using (var activity = _activitySource.StartActivity(
                                   "outbox.dispatch.DebtCompensatedEvent.Topic",
                                   ActivityKind.Producer,
                                   compensatedTopicContext))
                        {
                            await _rabbitMqPublisher.PublishToTopicAsync(
                                routingKey: $"debt.compensated.{compensatedTopic!.UserId}",
                                message: compensatedTopic);

                            _logger.LogInformation(
                                "Outbox message {Type} published to Topic Exchange",
                                message.Type);
                        }
                        break;

                    case $"{nameof(DebtCompensatedEvent)}.Fanout":
                        var compensatedFanout = JsonSerializer.Deserialize<DebtCompensatedEvent>(message.Payload);

                        ActivityContext compensatedFanoutContext = default;
                        if (!string.IsNullOrEmpty(message.TraceParent))
                            ActivityContext.TryParse(message.TraceParent, null, isRemote: true, out compensatedFanoutContext);

                        using (var activity = _activitySource.StartActivity(
                                   "outbox.dispatch.DebtCompensatedEvent.Fanout",
                                   ActivityKind.Producer,
                                   compensatedFanoutContext))
                        {
                            await _rabbitMqPublisher.PublishToFanoutAsync(
                                message: compensatedFanout!);

                            _logger.LogInformation(
                                "Outbox message {Type} published to Fanout Exchange",
                                message.Type);
                        }
                        break;
                }

                message.ProcessedOnUtc = DateTime.UtcNow;
                
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing outbox message {MessageId}",
                    message.Id);

                message.Error = ex.Message;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}