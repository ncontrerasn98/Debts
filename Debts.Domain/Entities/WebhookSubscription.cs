namespace Debts.Domain.Entities;

public class WebhookSubscription
{
    public Guid Id { get; private set; }
    public string Url { get; private set; }
    public string EventType { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ICollection<WebhookDeliveryAttempt> DeliveryAttempts { get; set; } = new List<WebhookDeliveryAttempt>();

    public WebhookSubscription() { }

    public WebhookSubscription(string url, string eventType)
    {
        Id = Guid.NewGuid();
        Url = url;
        EventType = eventType;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
}
