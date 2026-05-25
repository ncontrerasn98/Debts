using Debts.Application.Abstractions.Auth;
using Debts.Application.Abstractions.Persistence;
using Debts.Domain.Entities;
using Debts.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Debts.Application.Commands.CreateUser;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<CreateUserHandler> _logger;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    
    public CreateUserHandler(IUserRepository userRepository, ILogger<CreateUserHandler> logger, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetByNameAsync(command.Name);

        if (existingUser != null)
        {
            throw new DomainException("User already exists");
        }
        
        var passwordHash = _passwordHasher.Hash(command.Password);
        var user = new User(command.Name, passwordHash);

        await _userRepository.AddAsync(user);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation(
            "Creating user {Id} with name {Name}",
            user.Id, command.Name);

        return user.Id;
    }
 
}