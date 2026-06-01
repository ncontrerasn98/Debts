using System.Text.Json;
using Confluent.Kafka;
using Debts.Application.Abstractions.Audit;
using Debts.Application.Abstractions.Persistence;
using Debts.Application.Events;
using Debts.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Debts.Infrastructure.Persistence.Messaging.Consumers;

public class DebtSettlementReversedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DebtSettlementReversedConsumer> _logger;
    private readonly IConfiguration _configuration;

    public DebtSettlementReversedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<DebtSettlementReversedConsumer> logger, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Factory.StartNew(async () =>
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"],
                GroupId = "debts-settlement-reversed-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe("debt-settlement-reversed");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var result = consumer.Consume(stoppingToken);

                    try
                    {
                        var reversedEvent = JsonSerializer.Deserialize<DebtSettlementReversedEvent>(
                            result.Message.Value);

                        using var scope = _scopeFactory.CreateScope();
                        var debtRepository = scope.ServiceProvider
                            .GetRequiredService<IDebtRepository>();
                        var auditService = scope.ServiceProvider
                            .GetRequiredService<IAuditService>();
                        var unitOfWork = scope.ServiceProvider
                            .GetRequiredService<IUnitOfWork>();

                        var debt = await debtRepository
                            .GetTrackedByIdAsync(reversedEvent!.DebtId);

                        if (debt is null)
                        {
                            _logger.LogWarning(
                                "Debt {DebtId} not found for reversal",
                                reversedEvent.DebtId);
                            consumer.Commit(result);
                            continue;
                        }

                        debt.MarkAsUnsettled();
                        await unitOfWork.SaveChangesAsync(stoppingToken);

                        await auditService.LogAsync(
                            action: AuditLog.Actions.SettlementReversed,
                            entityName: nameof(Debt),
                            entityId: debt.Id,
                            details: $"Settlement reversed by CreditScore saga compensation",
                            cancellationToken: stoppingToken);

                        consumer.Commit(result);

                        _logger.LogWarning(
                            "Saga compensation applied — debt {DebtId} marked as unsettled",
                            reversedEvent.DebtId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error processing DebtSettlementReversedEvent offset {Offset}",
                            result.Offset.Value);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("DebtSettlementReversed consumer stopping");
            }
            finally
            {
                consumer.Close();
            }

        }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
    }
}