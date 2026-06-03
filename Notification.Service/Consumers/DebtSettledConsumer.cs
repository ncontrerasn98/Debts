// Notification.Service/Consumers/DebtSettledConsumer.cs
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Notification.Service.Data;
using Notification.Service.Hubs;
using Notification.Service.Services;
using Shared.Contracts.Events;

namespace Notification.Service.Consumers;

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
                GroupId = "notification-debt-settled-group",
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
                    
                    Console.WriteLine($"🟡 Notification DebtSettledConsumer received message: {result.Message.Value}");

                    try
                    {
                        var debtSettledEvent = JsonSerializer.Deserialize<DebtSettledEvent>(
                            result.Message.Value);

                        using var scope = _scopeFactory.CreateScope();
                        var dbContext = scope.ServiceProvider
                            .GetRequiredService<NotificationDbContext>();
                        var emailSender = scope.ServiceProvider
                            .GetRequiredService<IEmailSender>();
                        var hubContext = scope.ServiceProvider
                            .GetRequiredService<IHubContext<NotificationHub>>();

                        // Verificar preferencias del usuario
                        var preferences = await dbContext.NotificationPreferences
                            .FirstOrDefaultAsync(p => p.UserId == debtSettledEvent!.UserId);

                        // Si no tiene preferencias, usar defaults (email + signalR)
                        var emailEnabled = preferences?.EmailEnabled ?? true;
                        var signalREnabled = preferences?.SignalREnabled ?? true;

                        // Mandar email
                        if (emailEnabled)
                        {
                            await emailSender.SendAsync(
                                to: "test-mail@email.com",
                                subject: "Your debt has been settled",
                                body: $"""
                                    <h2>Debt Settled</h2>
                                    <p>Your debt <strong>{debtSettledEvent!.DebtId}</strong> 
                                    was settled on {debtSettledEvent.SettledAt:dd MMM yyyy HH:mm} UTC.</p>
                                    <p>Thank you for your payment.</p>
                                    """);

                            dbContext.Notifications.Add(new Entities.Notification(
                                debtSettledEvent!.UserId,
                                Entities.Notification.Types.DebtSettled,
                                Entities.Notification.Channels.Email,
                                $"Debt {debtSettledEvent.DebtId} settled"));
                        }

                        // Notificar via SignalR
                        if (signalREnabled)
                        {
                            await hubContext.Clients
                                .Group($"user-{debtSettledEvent!.UserId}")
                                .SendAsync("DebtSettled", new
                                {
                                    DebtId = debtSettledEvent.DebtId,
                                    Amount = debtSettledEvent.OriginalAmount,
                                    SettledAt = debtSettledEvent.SettledAt
                                }, stoppingToken);

                            dbContext.Notifications.Add(new Entities.Notification(
                                debtSettledEvent!.UserId,
                                Entities.Notification.Types.DebtSettled,
                                Entities.Notification.Channels.SignalR,
                                $"Debt {debtSettledEvent.DebtId} settled"));
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);
                        consumer.Commit(result);

                        _logger.LogInformation(
                            "Notifications sent for debt {DebtId}",
                            debtSettledEvent!.DebtId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error processing DebtSettledEvent offset {Offset}",
                            result.Offset.Value);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("DebtSettled notification consumer stopping");
            }
            finally
            {
                consumer.Close();
            }

        }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
    }
}