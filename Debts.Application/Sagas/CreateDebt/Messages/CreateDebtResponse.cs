namespace Debts.Application.Sagas.CreateDebt.Messages;

public record CreateDebtResponse
{
    public Guid DebtId { get; init; }
    public bool Success { get; init; }
    public string? FailureReason { get; init; }
}