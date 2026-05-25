namespace Debts.Application.Messaging.Commands;

public record SendDebtSettledEmailCommand
{
    public Guid MessageId { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid DebtId { get; init; }
    public string Email { get; init; } = default!;
    public DateTime SettledAt { get; init; }
}