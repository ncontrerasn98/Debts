using Debts.Application.Abstractions.Auth;
using Microsoft.Extensions.Caching.Distributed;

namespace Debts.Infrastructure.Persistence.Auth;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IDistributedCache _cache;

    public TokenBlacklistService(IDistributedCache cache)
        => _cache = cache;

    public async Task BlacklistAsync(string token, TimeSpan expiry)
        => await _cache.SetStringAsync(
            $"blacklist:{token}",
            "revoked",
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            });

    public async Task<bool> IsBlacklistedAsync(string token)
        => await _cache.GetStringAsync($"blacklist:{token}") is not null;
}