namespace Debts.Application.Abstractions.Auth;

public interface ITokenProvider
{
    string GenerateToken(Guid userId, string name, IEnumerable<string> roles);
    string GenerateRefreshToken();
}