using Debts.Application.DTOs;
using MediatR;

namespace Debts.Application.Commands.Auth.Login;

public class LoginCommand : IRequest<AuthResponse>
{
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}