using Debts.Application.Abstractions.Auth;
using Debts.Application.Abstractions.Persistence;
using Debts.Application.DTOs;
using Debts.Domain.Exceptions;
using MediatR;

namespace Debts.Application.Commands.Auth.RefreshToken;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _ruserRepository;
    private readonly ITokenProvider _tokenProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenHandler(IRefreshTokenRepository refreshTokenRepository, ITokenProvider tokenProvider, IUserRepository ruserRepository, IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _tokenProvider = tokenProvider;
        _ruserRepository = ruserRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(
            request.RefreshToken, cancellationToken);

        if (storedToken is null)
            throw new UnauthorizedException("Invalid refresh token");

        // Token revocado o comprometido — posible reuso detectado
        if (!storedToken.IsActive)
        {
            // Si el token pertenece a una familia, invalidarla completa
            await _refreshTokenRepository.RevokeAllFamilyAsync(
                storedToken.FamilyId, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedException("Refresh token reuse detected — all sessions invalidated. Please log in again.");
        }

        // Revocar el token actual y vincularlo con el nuevo
        var user = await _ruserRepository.GetByIdAsync(storedToken.UserId);
        var roles = user.UserRoles.Select(ur => ur.Role.Name);

        var newAccessToken = _tokenProvider.GenerateToken(user.Id, user.Name, roles);
        var newRefreshToken = _tokenProvider.GenerateRefreshToken();

        // Revocar v_actual y registrar qué token lo reemplazó
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByToken = newRefreshToken;

        // Nuevo token hereda el FamilyId — misma familia, siguiente eslabón
        var refreshTokenEntity = new Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = newRefreshToken,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            FamilyId = storedToken.FamilyId,    // ← hereda la familia
            ReplacedByToken = null              // aún no fue reemplazado
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };
    }
}