namespace Debts.Application.Abstractions.Idempotency;

public interface IIdempotencyService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string response, TimeSpan? ttl = null);
}