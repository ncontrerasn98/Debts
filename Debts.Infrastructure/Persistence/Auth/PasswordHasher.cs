using Debts.Application.Abstractions.Auth;
using Microsoft.Extensions.Configuration;

namespace Debts.Infrastructure.Persistence.Auth;

public class PasswordHasher : IPasswordHasher
{
    private readonly string _pepper;

    public PasswordHasher(IConfiguration configuration)
    {
        _pepper = configuration["Auth:Pepper"]
                  ?? throw new InvalidOperationException("Auth:Pepper not configured");
    }

    public string Hash(string password)
    {
        // BCrypt genera y embebe el salt automáticamente
        return BCrypt.Net.BCrypt.HashPassword(password + _pepper, workFactor: 12);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password + _pepper, hash);
    }
}