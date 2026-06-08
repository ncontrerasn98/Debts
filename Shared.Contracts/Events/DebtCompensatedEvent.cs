namespace Shared.Contracts.Events;

public class DebtCompensatedEvent
{
    public Guid DebtId { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CompensatedAt { get; set; }
}