using Debts.Domain.Entities;

namespace Debts.Application.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct);
    Task AddAsync(RefreshToken token, CancellationToken ct);
    Task<IEnumerable<RefreshToken>> GetFamilyAsync(Guid familyId, CancellationToken ct);
    Task RevokeAllFamilyAsync(Guid familyId, CancellationToken ct);
}