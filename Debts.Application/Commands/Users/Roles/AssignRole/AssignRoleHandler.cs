using Debts.Application.Abstractions.Persistence;
using Debts.Domain.Entities;
using Debts.Domain.Exceptions;
using MediatR;

namespace Debts.Application.Commands.Users.AssignRole;

public class AssignRoleHandler : IRequestHandler<AssignRoleCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignRoleHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AssignRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId);
        if (user is null)
            throw new NotFoundException("User not found");

        var role = await _roleRepository.GetByNameAsync(command.RoleName);
        if (role is null)
            throw new NotFoundException($"Role {command.RoleName} not found");

        var alreadyAssigned = await _roleRepository.UserHasRoleAsync(user.Id, role.Id);
        if (alreadyAssigned)
            throw new DomainException($"User already has role {command.RoleName}");

        await _roleRepository.AssignRoleAsync(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}