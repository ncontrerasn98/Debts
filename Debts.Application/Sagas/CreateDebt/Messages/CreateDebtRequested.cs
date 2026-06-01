namespace Debts.Application.Sagas.CreateDebt.Messages;

public record CreateDebtRequested
{
    public Guid CorrelationId { get; init; }
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
    public Guid RequestedBy { get; init; }
}

public record DebtCreated
{
    public Guid CorrelationId { get; init; }
    public Guid DebtId { get; init; }
}

public record DebtCreationFailed
{
    public Guid CorrelationId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public record DebtCreationCompensated
{
    public Guid CorrelationId { get; init; }
}