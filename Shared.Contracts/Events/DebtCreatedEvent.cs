namespace Shared.Contracts.Events;

public class DebtCreatedEvent
{
    public Guid DebtId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Region { get; set; } = "co";
}