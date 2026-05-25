using System.Text.Json;
using Debts.Application.Abstractions.CreditScore;
using Debts.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;

namespace Debts.Infrastructure.CreditScore;

public class CreditScoreService : ICreditScoreService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CreditScoreService> _logger;

    public CreditScoreService(HttpClient httpClient, ILogger<CreditScoreService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CreditScoreResult?> GetScoreAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;

        response = await _httpClient.GetAsync(
            $"/credit-score/{userId}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            _logger.LogWarning("CreditScore circuit breaker is open — fallback activated");
            throw new ServiceUnavailableException("Circuit breaker is open — service temporarily blocked");
        }

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonSerializer.Deserialize<CreditScoreResult>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}