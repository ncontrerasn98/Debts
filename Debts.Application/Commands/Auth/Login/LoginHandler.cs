using Debts.Application.Abstractions.Auth;
using Debts.Application.Abstractions.Persistence;
using Debts.Application.DTOs;
using Debts.Domain.Exceptions;
using MediatR;

namespace Debts.Application.Commands.Auth.Login;

public class LoginHandler: IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenProvider _tokenProvider;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LoginHandler(
        IUserRepository userRepository,
        ITokenProvider jwtTokenGenerator, IRefreshTokenRepository refreshTokenRepository, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _tokenProvider = jwtTokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponse> Handle(
        LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByNameAsync(command.Name);

        if (user is null)
        {
            throw new NotFoundException(
                "User not found");
        }
        
        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid credentials");
        
        var roles = user.UserRoles.Select(ur => ur.Role.Name);
        var accessToken = _tokenProvider.GenerateToken(user.Id, user.Name, roles);
        var refreshToken = _tokenProvider.GenerateRefreshToken();
        
        var refreshEntity = new Domain.Entities.RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _refreshTokenRepository.AddAsync(refreshEntity, CancellationToken.None);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

}