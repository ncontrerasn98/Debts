using System.Text.Json;
using Debts.Application.Abstractions.Audit;
using Debts.Application.Abstractions.Persistence;
using Debts.Application.Sagas.CreateDebt;
using Debts.Application.Sagas.CreateDebt.Messages;
using Debts.Domain.Entities;
using MassTransit;
using Shared.Contracts.Events;

namespace Debts.Application.Sagas;

public class CompensateDebtCreationActivity :
    IStateMachineActivity<DebtCreationSagaState, DebtCreationFailed>
{
    private readonly IDebtRepository _debtRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxMessagesRepository _outboxMessagesRepository;

    public CompensateDebtCreationActivity(
        IDebtRepository debtRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork, IOutboxMessagesRepository outboxMessagesRepository)
    {
        _debtRepository = debtRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _outboxMessagesRepository = outboxMessagesRepository;
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

                // Eliminar OutboxMessages pendientes
                await _outboxMessagesRepository.DeletePendingAsync(
                    correlationId: context.Saga.DebtId.Value.ToString());

                // Insertar evento de compensación para los mensajes que ya se publicaron
                var compensatedEvent = new DebtCompensatedEvent
                {
                    DebtId = context.Saga.DebtId.Value,
                    UserId = context.Saga.UserId,
                    Reason = context.Saga.FailureReason,
                    CompensatedAt = DateTime.UtcNow
                };

                var serializedEvent = JsonSerializer.Serialize(compensatedEvent);

                // Topic — para CreditScore.Api y Notification.Service
                await _outboxMessagesRepository.AddAsync(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = nameof(DebtCompensatedEvent),
                    Payload = serializedEvent,
                    OccurredOnUtc = DateTime.UtcNow,
                    CorrelationId = context.Saga.DebtId.Value.ToString()
                }, CancellationToken.None);

                // Fanout — broadcast a todos los servicios
                await _outboxMessagesRepository.AddAsync(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = $"{nameof(DebtCompensatedEvent)}.Fanout",
                    Payload = serializedEvent,
                    OccurredOnUtc = DateTime.UtcNow,
                    CorrelationId = context.Saga.DebtId.Value.ToString()
                }, CancellationToken.None);

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