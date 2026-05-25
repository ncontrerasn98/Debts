using MediatR;

namespace Debts.Application.Commands.Users.AssignRole;

public class AssignRoleCommand : IRequest
{
    public Guid UserId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}