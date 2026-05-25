namespace Debts.Domain.Entities;

public class WebhookDeliveryAttempt
{
    public Guid Id { get; private set; }
    public Guid WebhookSubscriptionId { get; private set; }
    public WebhookSubscription Subscription { get; set; } = null!;
    public string Payload { get; private set; }
    public int AttemptNumber { get; private set; }
    public bool Success { get; private set; }
    public string? Error { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? NextRetryAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    public WebhookDeliveryAttempt() { }

    public WebhookDeliveryAttempt(Guid subscriptionId, string payload)
    {
        Id = Guid.NewGuid();
        WebhookSubscriptionId = subscriptionId;
        Payload = payload;
        AttemptNumber = 0;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkSuccess()
    {
        Success = true;
        DeliveredAt = DateTime.UtcNow;
        NextRetryAt = null;
    }

    public void MarkFailed(string error)
    {
        AttemptNumber++;
        Success = false;
        Error = error;

        if (AttemptNumber < 3)
            NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, AttemptNumber - 1));
        else
            NextRetryAt = null; // se abandona
    }
}