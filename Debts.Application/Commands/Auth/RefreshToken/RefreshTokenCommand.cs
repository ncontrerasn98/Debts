using Debts.Application.DTOs;
using MediatR;

namespace Debts.Application.Commands.Auth.RefreshToken;

public class RefreshTokenCommand : IRequest<AuthResponse>
{
    public string RefreshToken { get; set; } = string.Empty;
}