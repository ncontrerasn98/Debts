// Debts.Application/Sagas/CreateDebt/DebtCreationSaga.cs
using Debts.Application.Sagas.CreateDebt.Messages;
using MassTransit;

namespace Debts.Application.Sagas.CreateDebt;

public class DebtCreationSaga : MassTransitStateMachine<DebtCreationSagaState>
{
    // Estados
    public State Creating { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Compensating { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    // Eventos
    public Event<CreateDebtRequested> CreateDebtRequested { get; private set; } = null!;
    public Event<DebtCreated> DebtCreated { get; private set; } = null!;
    public Event<DebtCreationFailed> DebtCreationFailed { get; private set; } = null!;
    public Event<DebtCreationCompensated> DebtCreationCompensated { get; private set; } = null!;

    public DebtCreationSaga()
    {
        InstanceState(x => x.CurrentState);

        // Correlación por CorrelationId
        Event(() => CreateDebtRequested, x =>
            x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => DebtCreated, x =>
            x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => DebtCreationFailed, x =>
            x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => DebtCreationCompensated, x =>
            x.CorrelateById(ctx => ctx.Message.CorrelationId));

        // Flujo principal
        Initially(
            When(CreateDebtRequested)
                .Then(ctx =>
                {
                    Console.WriteLine($"🟡 Saga received CreateDebtRequested for {ctx.Message.CorrelationId}");
                    ctx.Saga.UserId = ctx.Message.UserId;
                    ctx.Saga.Amount = ctx.Message.Amount;
                    ctx.Saga.RequestedBy = ctx.Message.RequestedBy;
                    ctx.Saga.CreatedAt = DateTime.UtcNow;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                    ctx.Saga.RequestId = ctx.RequestId;
                    ctx.Saga.ResponseAddress = ctx.ResponseAddress;
                })
                .TransitionTo(Creating)
                .Activity(x => x.OfType<CreateDebtActivity>()));

        During(Creating,
            When(DebtCreated)
                .Then(ctx =>
                {
                    ctx.Saga.DebtId = ctx.Message.DebtId;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .ThenAsync(async ctx =>
                {
                    var endpoint = await ctx.GetSendEndpoint(ctx.Saga.ResponseAddress!);
                    await endpoint.Send(new CreateDebtResponse
                    {
                        DebtId = ctx.Saga.DebtId!.Value,
                        Success = true
                    }, x => x.RequestId = ctx.Saga.RequestId);
                })
                .TransitionTo(Completed)
                .Finalize(),

            When(DebtCreationFailed)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason = ctx.Message.Reason;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .ThenAsync(async ctx =>
                {
                    var endpoint = await ctx.GetSendEndpoint(ctx.Saga.ResponseAddress!);
                    await endpoint.Send(new CreateDebtResponse
                    {
                        Success = false,
                        FailureReason = ctx.Saga.FailureReason
                    }, x => x.RequestId = ctx.Saga.RequestId);
                })
                .TransitionTo(Compensating)
                .Activity(x => x.OfType<CompensateDebtCreationActivity>()));
        During(Compensating,
            When(DebtCreationCompensated)
                .Then(ctx => ctx.Saga.UpdatedAt = DateTime.UtcNow)
                .TransitionTo(Failed)
                .Finalize());

        // SetCompletedWhenFinalized();
    }
}