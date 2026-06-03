namespace Shared.Contracts.Events;

public record CreditScoreUpdatedEvent
{
    public Guid UserId { get; init; }
    public int Score { get; init; }
    public string Rating { get; init; } = string.Empty;
    public bool IsLowScore { get; init; } // score < 400
    public DateTime UpdatedAt { get; init; }
}