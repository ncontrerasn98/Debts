using Debts.Application.Abstractions.Audit;
using Debts.Application.Abstractions.Auth;
using Debts.Application.Abstractions.CreditScore;
using Debts.Application.Abstractions.Persistence;
using Debts.Domain.Entities;
using Debts.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Debts.Application.Commands.Debts.CreateDebt;

public class CreateDebtHandler : IRequestHandler<CreateDebtCommand, Guid>
{
    private readonly IDebtRepository _debtRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICreditScoreService _creditScoreService;
    private readonly ILogger<CreateDebtHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService  _currentUserService;

    public CreateDebtHandler(IDebtRepository debtRepository, IUserRepository userRepository, ILogger<CreateDebtHandler> logger, IUnitOfWork unitOfWork, ICreditScoreService creditScoreService, IAuditService auditService, ICurrentUserService currentUserService)
    {
        _debtRepository = debtRepository;
        _userRepository = userRepository;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _creditScoreService = creditScoreService;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateDebtCommand command, CancellationToken cancellationToken)
    {
        var userExists = await _userRepository.ExistsAsync(command.UserId);

        if (!userExists)
        {
            throw new NotFoundException("User does not exist");
        }
        
        try
        {
            var creditScore = await _creditScoreService.GetScoreAsync(
                command.UserId,
                cancellationToken);

            if (creditScore is not null && creditScore.Score < 400)
            {
                _logger.LogWarning(
                    "Debt creation rejected for user {UserId} — score {Score} too low",
                    command.UserId,
                    creditScore.Score);

                throw new DomainException(
                    $"Credit score too low to create a debt — current score: {creditScore.Score} ({creditScore.Rating})");
            }

            if (creditScore is not null)
            {
                _logger.LogInformation(
                    "Credit score check passed for user {UserId} — score {Score} ({Rating})",
                    command.UserId,
                    creditScore.Score,
                    creditScore.Rating);
            }
        }
        catch (ServiceUnavailableException)
        {
            // Si el servicio de score está caído, permitimos la operación
            _logger.LogWarning(
                "Credit score service unavailable — allowing debt creation for user {UserId}",
                command.UserId);
        }
        
        var debt = new Debt(command.Amount, command.UserId);

        await _debtRepository.AddAsync(debt);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(
            action: AuditLog.Actions.Created,
            entityName: nameof(Debt),
            entityId: debt.Id,
            details: $"Amount: {command.Amount}, DebtOwner: {command.UserId}, CreatedBy: {_currentUserService.UserId}",
            cancellationToken: cancellationToken);
        
        _logger.LogInformation(
            "Creating debt for user {UserId} with amount {Amount}",
            command.UserId,
            command.Amount);

        return debt.Id;
    }
    
}