using System.Text;
using System.Text.Json;
using Debts.Application.Abstractions.Webhooks;
using Debts.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Debts.Infrastructure.Webhooks;

public class WebhookDispatcher : IWebhookDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookDispatcher> _logger;

    public WebhookDispatcher(HttpClient httpClient, ILogger<WebhookDispatcher> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task DispatchAsync(WebhookDeliveryAttempt attempt, CancellationToken cancellationToken = default)
    {
        var content = new StringContent(
            attempt.Payload,
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            attempt.Subscription.Url,
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        _logger.LogInformation(
            "Webhook delivered to {Url} on attempt {AttemptNumber}",
            attempt.Subscription.Url,
            attempt.AttemptNumber);
    }
}