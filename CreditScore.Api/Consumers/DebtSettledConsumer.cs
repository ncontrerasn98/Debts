using System.Text.Json;
using Confluent.Kafka;
using CreditScore.Api.Data;
using CreditScore.Api.Entities;
using CreditScore.Api.Events;
using Microsoft.EntityFrameworkCore;

namespace CreditScore.Api.Consumers;

public class DebtSettledConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DebtSettledConsumer> _logger;

    public DebtSettledConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<DebtSettledConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Factory.StartNew(async () =>
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "credit-score-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe("debt-settled");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var result = consumer.Consume(stoppingToken);

                    try
                    {
                        var debtSettledEvent = JsonSerializer.Deserialize<DebtSettledEvent>(
                            result.Message.Value);

                        using var scope = _scopeFactory.CreateScope();
                        var dbContext = scope.ServiceProvider
                            .GetRequiredService<CreditScoreDbContext>();

                        // buscar historial existente o crear uno nuevo
                        var history = await dbContext.CreditHistories
                            .FirstOrDefaultAsync(
                                h => h.UserId == debtSettledEvent!.UserId,
                                stoppingToken);

                        if (history is null)
                        {
                            history = new CreditHistory(debtSettledEvent!.UserId);
                            dbContext.CreditHistories.Add(history);
                        }

                        history.ApplyDebtSettled(
                            debtSettledEvent!.OriginalAmount,
                            debtSettledEvent.NegotiatedAmount,
                            debtSettledEvent.CreatedAt);

                        await dbContext.SaveChangesAsync(stoppingToken);

                        consumer.Commit(result);

                        _logger.LogInformation(
                            "Credit history updated for user {UserId}",
                            debtSettledEvent.UserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error processing DebtSettledEvent for offset {Offset}",
                            result.Offset.Value);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Credit score consumer stopping");
            }
            finally
            {
                consumer.Close();
            }

        }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
    }
}