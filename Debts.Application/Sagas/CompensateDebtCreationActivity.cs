using Debts.Application.Abstractions.Audit;
using Debts.Application.Abstractions.Persistence;
using Debts.Application.Sagas.CreateDebt;
using Debts.Application.Sagas.CreateDebt.Messages;
using Debts.Domain.Entities;
using MassTransit;

namespace Debts.Application.Sagas;

public class CompensateDebtCreationActivity :
    IStateMachineActivity<DebtCreationSagaState, DebtCreationFailed>
{
    private readonly IDebtRepository _debtRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    public CompensateDebtCreationActivity(
        IDebtRepository debtRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork)
    {
        _debtRepository = debtRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
    }

    public async Task Execute(
        BehaviorContext<DebtCreationSagaState, DebtCreationFailed> context,
        IBehavior<DebtCreationSagaState, DebtCreationFailed> next)
    {
        // Solo compensar si la deuda fue creada
        if (context.Saga.DebtId.HasValue)
        {
            var debt = await _debtRepository.GetTrackedByIdAsync(context.Saga.DebtId.Value);
            if (debt is not null)
            {
                _debtRepository.Remove(debt);
                await _unitOfWork.SaveChangesAsync();

                await _auditService.LogAsync(
                    action: AuditLog.Actions.Deleted,
                    entityName: nameof(Debt),
                    entityId: context.Saga.DebtId.Value,
                    details: $"Debt creation compensated — Reason: {context.Saga.FailureReason}");
            }
        }

        await context.Publish(new DebtCreationCompensated
        {
            CorrelationId = context.Saga.CorrelationId
        });

        await next.Execute(context);
    }

    public async Task Faulted<TException>(
        BehaviorExceptionContext<DebtCreationSagaState, DebtCreationFailed, TException> context,
        IBehavior<DebtCreationSagaState, DebtCreationFailed> next)
        where TException : Exception
        => await next.Faulted(context);

    public void Probe(ProbeContext context) => context.CreateScope("compensate-debt-creation");
    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}