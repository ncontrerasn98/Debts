// Notification.Service/Consumers/CreditScoreUpdatedConsumer.cs
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Notification.Service.Data;
using Notification.Service.Hubs;
using Notification.Service.Services;
using Shared.Contracts.Events;

namespace Notification.Service.Consumers;

public class CreditScoreUpdatedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CreditScoreUpdatedConsumer> _logger;

    public CreditScoreUpdatedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<CreditScoreUpdatedConsumer> logger)
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
                GroupId = "notification-credit-score-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe("credit-score-updated");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var result = consumer.Consume(stoppingToken);

                    try
                    {
                        var creditScoreEvent = JsonSerializer.Deserialize<CreditScoreUpdatedEvent>(
                            result.Message.Value);

                        using var scope = _scopeFactory.CreateScope();
                        var dbContext = scope.ServiceProvider
                            .GetRequiredService<NotificationDbContext>();
                        var emailSender = scope.ServiceProvider
                            .GetRequiredService<IEmailSender>();
                        var hubContext = scope.ServiceProvider
                            .GetRequiredService<IHubContext<NotificationHub>>();

                        var preferences = await dbContext.NotificationPreferences
                            .FirstOrDefaultAsync(p => p.UserId == creditScoreEvent!.UserId);

                        var emailEnabled = preferences?.EmailEnabled ?? true;
                        var signalREnabled = preferences?.SignalREnabled ?? true;
                        var threshold = preferences?.LowScoreThreshold ?? 400;

                        // Notificar via SignalR
                        if (signalREnabled)
                        {
                            await hubContext.Clients
                                .Group($"user-{creditScoreEvent!.UserId}")
                                .SendAsync("CreditScoreUpdated", new
                                {
                                    UserId = creditScoreEvent.UserId,
                                    Score = creditScoreEvent.Score,
                                    Rating = creditScoreEvent.Rating,
                                    UpdatedAt = creditScoreEvent.UpdatedAt
                                }, stoppingToken);

                            dbContext.Notifications.Add(new Entities.Notification(
                                creditScoreEvent!.UserId,
                                Entities.Notification.Types.CreditScoreUpdated,
                                Entities.Notification.Channels.SignalR,
                                $"Score updated to {creditScoreEvent.Score} ({creditScoreEvent.Rating})"));
                        }

                        // Email de alerta si score bajo
                        if (emailEnabled && creditScoreEvent!.Score < threshold)
                        {
                            await emailSender.SendAsync(
                                to: "test-mail@email.com",
                                subject: "⚠️ Your credit score is low",
                                body: $"""
                                    <h2>Low Credit Score Alert</h2>
                                    <p>Your current credit score is <strong>{creditScoreEvent.Score} ({creditScoreEvent.Rating})</strong>.</p>
                                    <p>A low score may prevent you from creating new debts.</p>
                                    <p>Consider settling existing debts to improve your score.</p>
                                    """);

                            dbContext.Notifications.Add(new Entities.Notification(
                                creditScoreEvent!.UserId,
                                Entities.Notification.Types.LowCreditScore,
                                Entities.Notification.Channels.Email,
                                $"Low score alert: {creditScoreEvent.Score}"));
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);
                        consumer.Commit(result);

                        _logger.LogInformation(
                            "Credit score notifications sent for user {UserId} — score {Score}",
                            creditScoreEvent!.UserId,
                            creditScoreEvent.Score);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error processing CreditScoreUpdatedEvent offset {Offset}",
                            result.Offset.Value);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("CreditScoreUpdated notification consumer stopping");
            }
            finally
            {
                consumer.Close();
            }

        }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
    }
}