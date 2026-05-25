using Polly;
using Polly.Extensions.Http;
using Polly.CircuitBreaker;
using Microsoft.Extensions.Logging;

namespace Debts.Infrastructure.CreditScore;

public static class CreditScoreResiliencePolicies
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timespan, attempt, _) =>
                    logger.LogWarning(
                        "CreditScore retry {Attempt} after {Delay}s — {Reason}",
                        attempt,
                        timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()));

    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger logger) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    Console.WriteLine($"🔴 CIRCUIT BREAKER OPEN for {duration.TotalSeconds}s");
                    logger.LogError(
                        "CreditScore circuit breaker OPEN for {Duration}s — {Reason}",
                        duration.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                },
                onReset: () =>
                {
                    Console.WriteLine("🟢 CIRCUIT BREAKER CLOSED");
                    logger.LogInformation("CreditScore circuit breaker CLOSED");
                },
                onHalfOpen: () =>
                {
                    Console.WriteLine("🟡 CIRCUIT BREAKER HALF-OPEN");
                    logger.LogInformation("CreditScore circuit breaker HALF-OPEN");
                });
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy() =>
        Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(3));

    public static IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy(ILogger logger) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<BrokenCircuitException>()
            .FallbackAsync(
                fallbackValue: new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable),
                onFallbackAsync: (outcome, _) =>
                {
                    logger.LogWarning(
                        "CreditScore fallback triggered — {Reason}",
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                    return Task.CompletedTask;
                });
}