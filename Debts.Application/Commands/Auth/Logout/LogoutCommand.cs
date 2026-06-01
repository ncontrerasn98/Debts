using MediatR;

namespace Debts.Application.Commands.Auth.Logout;

public class LogoutCommand : IRequest
{
    public string Token { get; set; } = string.Empty;
}