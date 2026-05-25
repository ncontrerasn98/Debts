using Debts.Application.Abstractions.Persistence;
using Debts.Domain.Exceptions;
using MediatR;

namespace Debts.Application.Commands.Users.RevokeRole;

public class RevokeRoleHandler : IRequestHandler<RevokeRoleCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeRoleHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RevokeRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId);
        if (user is null)
            throw new NotFoundException("User not found");

        var role = await _roleRepository.GetByNameAsync(command.RoleName);
        if (role is null)
            throw new NotFoundException($"Role {command.RoleName} not found");

        var hasRole = await _roleRepository.UserHasRoleAsync(user.Id, role.Id);
        if (!hasRole)
            throw new DomainException($"User does not have role {command.RoleName}");

        await _roleRepository.RevokeRoleAsync(user.Id, role.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}