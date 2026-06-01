namespace Debts.Application.Abstractions.Auth;

public interface ITokenBlacklistService
{
    Task BlacklistAsync(string token, TimeSpan expiry);
    Task<bool> IsBlacklistedAsync(string token);
}