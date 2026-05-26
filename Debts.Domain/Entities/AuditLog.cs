namespace Debts.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string UserName { get; private set; }
    public string Action { get; private set; }
    public string EntityName { get; private set; }
    public Guid EntityId { get; private set; }
    public string? Details { get; private set; }
    public DateTime OccurredOnUtc { get; private set; }

    public AuditLog() { }

    public AuditLog(
        Guid? userId,
        string userName,
        string action,
        string entityName,
        Guid entityId,
        string? details = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        UserName = userName;
        Action = action;
        EntityName = entityName;
        EntityId = entityId;
        Details = details;
        OccurredOnUtc = DateTime.UtcNow;
    }

    // Acciones estándar
    public static class Actions
    {
        public const string Created = "CREATED";
        public const string Updated = "UPDATED";
        public const string Deleted = "DELETED";
        public const string Settled = "SETTLED";
        public const string RoleAssigned = "ROLE_ASSIGNED";
        public const string RoleRevoked = "ROLE_REVOKED";
    }
}