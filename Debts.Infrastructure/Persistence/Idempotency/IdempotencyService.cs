// Debts.Infrastructure/Idempotency/IdempotencyService.cs

using System.Text;
using Debts.Application.Abstractions.Idempotency;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace Debts.Infrastructure.Persistence.Idempotency;

public class IdempotencyService : IIdempotencyService
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromHours(24);

    public IdempotencyService(IDistributedCache cache)
        => _cache = cache;

    public async Task<string?> GetAsync(string key)
    {
        var value = await _cache.GetAsync($"idempotency:{key}");
        return value is not null ? Encoding.UTF8.GetString(value) : null;
    }

    public async Task SetAsync(string key, string response, TimeSpan? ttl = null)
        => await _cache.SetAsync(
            $"idempotency:{key}",
            Encoding.UTF8.GetBytes(response),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? _defaultTtl
            });
}