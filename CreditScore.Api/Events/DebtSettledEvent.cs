namespace CreditScore.Api.Events;

public record DebtSettledEvent
{
    public Guid DebtId { get; init; }
    public Guid UserId { get; init; }
    public decimal OriginalAmount { get; init; }
    public decimal? NegotiatedAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime SettledAt { get; init; }
}