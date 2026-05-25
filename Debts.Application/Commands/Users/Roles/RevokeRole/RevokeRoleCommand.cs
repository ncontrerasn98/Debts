using MediatR;

namespace Debts.Application.Commands.Users.RevokeRole;

public class RevokeRoleCommand : IRequest
{
    public Guid UserId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}