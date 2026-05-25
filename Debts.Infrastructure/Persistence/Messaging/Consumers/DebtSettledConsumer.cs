using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Debts.Application.Abstractions.Webhooks;
using Debts.Application.DTOs;
using Debts.Application.Events;
using Debts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Debts.Infrastructure.Persistence.Messaging.Consumers;

public class DebtSettledConsumer : BackgroundService
{
    private static readonly ActivitySource _activitySource = new("KafkaConsumer.DebtSettled");
    private static readonly TextMapPropagator _propagator = Propagators.DefaultTextMapPropagator;

    private readonly ILogger<DebtSettledConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public DebtSettledConsumer(
        ILogger<DebtSettledConsumer> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task<Task<Task>> ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Factory.StartNew(async () =>
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "debts-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true,
                AutoCommitIntervalMs = 1000
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe("debt-settled");
            
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var result = consumer.Consume(stoppingToken);

                    var parentContext = _propagator.Extract(
                        default,
                        result.Message.Headers,
                        (headers, key) =>
                        {
                            var header = headers.FirstOrDefault(h => h.Key == key);
                            return header is not null
                                ? new[] { Encoding.UTF8.GetString(header.GetValueBytes()) }
                                : Enumerable.Empty<string>();
                        });

                    Baggage.Current = parentContext.Baggage;

                    using var activity = _activitySource.StartActivity(
                        "kafka.consume debt-settled",
                        ActivityKind.Consumer,
                        parentContext.ActivityContext);

                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var webhookDispatcher = scope.ServiceProvider.GetRequiredService<IWebhookDispatcher>();

                    var debtSettledEvent = JsonSerializer.Deserialize<DebtSettledEvent>(
                        result.Message.Value);

                    // Cargar la deuda con el usuario
                    var debt = await dbContext.Debts
                        .Include(d => d.User)
                        .FirstOrDefaultAsync(d => d.Id == debtSettledEvent!.DebtId, stoppingToken);

                    if (debt is null)
                    {
                        _logger.LogWarning("Debt {DebtId} not found", debtSettledEvent!.DebtId);
                        continue;
                    }

                    // Construir payload amigable
                    var payload = JsonSerializer.Serialize(new DebtSettledWebhookPayload
                    {
                        DebtId = debt.Id,
                        OriginalAmount = debt.OriginalAmount,
                        NegotiatedAmount = debt.NegotiatedAmount,
                        SettledAmount = debt.NegotiatedAmount ?? debt.OriginalAmount,
                        UserId = debt.UserId,
                        SettledAt = debtSettledEvent!.SettledAt
                    });

                    // Buscar suscripciones activas para este evento
                    var subscriptions = await dbContext.WebhookSubscriptions
                        .Where(s => s.EventType == "debt.settled" && s.IsActive)
                        .ToListAsync(stoppingToken);

                    foreach (var subscription in subscriptions)
                    {
                        var attempt = new WebhookDeliveryAttempt(subscription.Id, payload);
                        attempt.Subscription = subscription; 

                        try
                        {
                            await webhookDispatcher.DispatchAsync(attempt, stoppingToken);
                            attempt.MarkSuccess();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Webhook delivery failed for subscription {SubscriptionId}",
                                subscription.Id);

                            attempt.MarkFailed(ex.Message);
                        }

                        dbContext.WebhookDeliveryAttempts.Add(attempt);
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                    
                    consumer.Commit(result);

                    _logger.LogInformation(
                        "Event received and processed: {Message}",
                        result.Message.Value);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Kafka consumer stopping");
            }
            finally
            {
                consumer.Close();
            }
        }, stoppingToken);
    }
    
}