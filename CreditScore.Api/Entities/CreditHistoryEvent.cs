namespace CreditScore.Api.Entities;

public class CreditHistoryEvent
{
    public Guid Id { get; private set; }
    public Guid CreditHistoryId { get; private set; }
    public Guid DebtId { get; private set; }
    public decimal Amount { get; private set; }
    public string EventType { get; private set; } = default!;
    public DateTime OccurredAt { get; private set; }

    public static class Types
    {
        public const string Created = "created";
        public const string Compensated = "compensated";
    }

    public CreditHistoryEvent() { }

    public CreditHistoryEvent(Guid creditHistoryId, Guid debtId, decimal amount, string eventType)
    {
        Id = Guid.NewGuid();
        CreditHistoryId = creditHistoryId;
        DebtId = debtId;
        Amount = amount;
        EventType = eventType;
        OccurredAt = DateTime.UtcNow;
    }
}