using MediatR;

namespace Debts.Application.Commands.CreateUser;

public class CreateUserCommand : IRequest<Guid>
{
    public string Name { get; set; }
    public string Password { get; set; }
}