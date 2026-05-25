// Debts.Infrastructure/Webhooks/WebhookRetryWorker.cs
using Debts.Application.Abstractions.Webhooks;
using Debts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Debts.Infrastructure.Webhooks;

public class WebhookRetryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookRetryWorker> _logger;

    public WebhookRetryWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<WebhookRetryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IWebhookDispatcher>();

            // Buscar intentos fallidos que ya cumplieron su espera y no superaron 3 intentos
            var pendingRetries = await dbContext.WebhookDeliveryAttempts
                .Include(a => a.Subscription)
                .Where(a =>
                    !a.Success &&
                    a.AttemptNumber < 3 &&
                    a.NextRetryAt <= DateTime.UtcNow)
                .ToListAsync(stoppingToken);

            foreach (var attempt in pendingRetries)
            {
                try
                {
                    await dispatcher.DispatchAsync(attempt, stoppingToken);
                    attempt.MarkSuccess();
                }
                catch (Exception ex)
                {
                    attempt.MarkFailed(ex.Message);
                }
            }

            if (pendingRetries.Any())
                await dbContext.SaveChangesAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}