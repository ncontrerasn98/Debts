using Debts.Application.Abstractions.Audit;
using Debts.Application.Abstractions.Persistence;
using Debts.Application.Sagas.CreateDebt.Messages;
using Debts.Domain.Entities;
using MassTransit;

namespace Debts.Application.Sagas.CreateDebt;

public class CreateDebtActivity : IStateMachineActivity<DebtCreationSagaState, CreateDebtRequested>
{
    private readonly IDebtRepository _debtRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDebtActivity(
        IDebtRepository debtRepository,
        IUserRepository userRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork)
    {
        _debtRepository = debtRepository;
        _userRepository = userRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
    }

    public async Task Execute(
        BehaviorContext<DebtCreationSagaState, CreateDebtRequested> context,
        IBehavior<DebtCreationSagaState, CreateDebtRequested> next)
    {
        Console.WriteLine($"🟡 CreateDebtActivity executing for {context.Saga.CorrelationId}");

        try
        {
            var userExists = await _userRepository.ExistsAsync(context.Saga.UserId);
            Console.WriteLine($"🟡 UserExists: {userExists}");
        
            if (!userExists)
            {
                Console.WriteLine($"🔴 User not found — publishing DebtCreationFailed");
                await context.Publish(new DebtCreationFailed
                {
                    CorrelationId = context.Saga.CorrelationId,
                    Reason = "User does not exist"
                });
                await next.Execute(context);
                return;
            }

            var debt = new Debt(context.Saga.Amount, context.Saga.UserId);
            await _debtRepository.AddAsync(debt);
            await _unitOfWork.SaveChangesAsync();
            Console.WriteLine($"🟢 Debt created: {debt.Id}");

            await _auditService.LogAsync(
                action: AuditLog.Actions.Created,
                entityName: nameof(Debt),
                entityId: debt.Id,
                details: $"Amount: {context.Saga.Amount}, DebtOwner: {context.Saga.UserId}, CreatedBy: {context.Saga.RequestedBy}");

            Console.WriteLine($"🟢 Publishing DebtCreated for {context.Saga.CorrelationId}");
            await context.Publish(new DebtCreated
            {
                CorrelationId = context.Saga.CorrelationId,
                DebtId = debt.Id
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🔴 Exception: {ex.Message}");
            await context.Publish(new DebtCreationFailed
            {
                CorrelationId = context.Saga.CorrelationId,
                Reason = ex.Message
            });
        }

        await next.Execute(context);
    }

    public async Task Faulted<TException>(
        BehaviorExceptionContext<DebtCreationSagaState, CreateDebtRequested, TException> context,
        IBehavior<DebtCreationSagaState, CreateDebtRequested> next)
        where TException : Exception
        => await next.Faulted(context);

    public void Probe(ProbeContext context) => context.CreateScope("create-debt");
    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}