namespace Debts.Application.DTOs;

public record DebtSettledWebhookPayload
{
    public string Event { get; init; } = "debt.settled";
    public Guid DebtId { get; init; }
    public decimal OriginalAmount { get; init; }
    public decimal? NegotiatedAmount { get; init; }
    public decimal SettledAmount { get; init; } // NegotiatedAmount ?? OriginalAmount
    public Guid UserId { get; init; }
    public DateTime SettledAt { get; init; }
}