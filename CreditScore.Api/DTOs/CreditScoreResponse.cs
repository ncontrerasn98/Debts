namespace CreditScore.Api.DTOs;

public record CreditScoreResponse(
    Guid UserId,
    int Score,
    string Rating,
    int SettledDebts,
    int ActiveDebts,
    DateTime UpdatedAt
);