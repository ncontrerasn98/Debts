namespace Shared.Contracts.Events;

public record DebtSettlementReversedEvent
{
    public Guid DebtId { get; init; }
    public Guid UserId { get; init; }
    public decimal OriginalAmount { get; init; }
    public decimal? NegotiatedAmount { get; init; }
    public DateTime ReversedAt { get; init; }
}