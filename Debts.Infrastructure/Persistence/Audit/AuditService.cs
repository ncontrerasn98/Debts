using Debts.Application.Abstractions.Audit;
using Debts.Application.Abstractions.Auth;
using Debts.Domain.Entities;

namespace Debts.Infrastructure.Persistence.Audit;

public class AuditService : IAuditService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public AuditService(AppDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task LogAsync(
        string action,
        string entityName,
        Guid entityId,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog(
            userId: _currentUserService.UserId,
            userName: _currentUserService.Name ?? "Unknown",
            action: action,
            entityName: entityName,
            entityId: entityId,
            details: details);

        await _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}