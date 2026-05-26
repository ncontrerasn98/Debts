using System.Diagnostics;
using System.Text.Json;
using Debts.Application.Abstractions.Audit;
using Debts.Application.Abstractions.Auth;
using Debts.Application.Abstractions.Persistence;
using Debts.Application.Commands.SettleDebt;
using Debts.Application.Events;
using Debts.Application.Messaging.Commands;
using Debts.Domain.Entities;
using Debts.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Debts.Application.Commands.Debts.SettleDebt;

public class SettleDebtHandler  : IRequestHandler<SettleDebtCommand, Unit>
{
    private readonly IDebtRepository _debtRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<SettleDebtHandler> _logger;
    private readonly IOutboxMessagesRepository _outboxMessagesRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService  _currentUserService;

    public SettleDebtHandler(
        IDebtRepository debtRepository,
        IDistributedCache cache,
        ILogger<SettleDebtHandler> logger, 
        IOutboxMessagesRepository outboxMessagesRepository, IUnitOfWork unitOfWork, IAuditService auditService, ICurrentUserService currentUserService)
    {
        _debtRepository = debtRepository;
        _cache = cache;
        _logger = logger;
        _outboxMessagesRepository = outboxMessagesRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(SettleDebtCommand command, CancellationToken cancellationToken)
    {
        var debt = await _debtRepository.GetTrackedByIdAsync(command.DebtId);

        if (debt is null)
        {
            throw new NotFoundException("Debt not found");
        }

        debt.MarkAsSettled();
        
        var traceParent = Activity.Current?.Id;
        
        var debtSettledEvent = new DebtSettledEvent
        {
            DebtId = debt.Id,
            UserId = debt.UserId,
            OriginalAmount = debt.OriginalAmount,
            NegotiatedAmount = debt.NegotiatedAmount,
            CreatedAt = debt.CreatedAt,
            SettledAt = DateTime.UtcNow
        };
        
        var outboxEventMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(DebtSettledEvent),
            Payload = JsonSerializer.Serialize(debtSettledEvent),
            OccurredOnUtc = DateTime.UtcNow,
            CorrelationId = debt.Id.ToString(),
            TraceParent =  traceParent
        };

        var debtSettledSendEmailCommand = new SendDebtSettledEmailCommand
        {
            MessageId =  Guid.NewGuid(),
            CorrelationId = debt.Id,
            DebtId = debt.Id,
            SettledAt = DateTime.UtcNow,
            Email = "test-mail@email.com"
        };
        
        var outboxCommandMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(SendDebtSettledEmailCommand),
            Payload = JsonSerializer.Serialize(debtSettledSendEmailCommand),
            OccurredOnUtc = DateTime.UtcNow,
            CorrelationId = debt.Id.ToString(),
            TraceParent =  traceParent
        };

        await _outboxMessagesRepository.AddAsync(outboxEventMessage, cancellationToken);
        await _outboxMessagesRepository.AddAsync(outboxCommandMessage, cancellationToken);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            action: AuditLog.Actions.Settled,
            entityName: nameof(Debt),
            entityId: debt.Id,
            details: $"Amount: {debt.OriginalAmount}, DebtOwner: {debt.UserId}, SettledBy: {_currentUserService.UserId}",
            cancellationToken: cancellationToken);
        
        var cacheKey = $"debt:{command.DebtId}";

        await _cache.RemoveAsync(cacheKey);

        _logger.LogInformation(
            "Cache invalidated for debt {DebtId}",
            command.DebtId);

        return Unit.Value;
    }

}