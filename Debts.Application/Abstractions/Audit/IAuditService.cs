namespace Debts.Application.Abstractions.Audit;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityName,
        Guid entityId,
        string? details = null,
        CancellationToken cancellationToken = default);
}