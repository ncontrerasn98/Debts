namespace Debts.Application.Abstractions.CreditScore;

public record CreditScoreResult(
    Guid UserId,
    int Score,
    string Rating,
    int SettledDebts,
    int ActiveDebts,
    DateTime UpdatedAt
);

public interface ICreditScoreService
{
    Task<CreditScoreResult?> GetScoreAsync(Guid userId, CancellationToken cancellationToken = default);
}