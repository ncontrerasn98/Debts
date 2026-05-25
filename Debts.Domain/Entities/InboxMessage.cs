namespace Debts.Domain.Entities;

public class InboxMessage
{
    public Guid MessageId { get; set; }

    public string Consumer { get; set; } = default!;

    public DateTime ProcessedOnUtc { get; set; }
}