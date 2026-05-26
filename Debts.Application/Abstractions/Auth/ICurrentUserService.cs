namespace Debts.Application.Abstractions.Auth;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Name { get; }
}