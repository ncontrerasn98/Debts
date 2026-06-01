using MassTransit;

namespace Debts.Application.Sagas.CreateDebt;

public class DebtCreationSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public Guid? DebtId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public Guid RequestedBy { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? RequestId { get; set; }        // <-- nuevo
    public Uri? ResponseAddress { get; set; }
}