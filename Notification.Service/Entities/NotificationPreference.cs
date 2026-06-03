namespace Notification.Service.Entities;

public class NotificationPreference
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public bool EmailEnabled { get; private set; }
    public bool SignalREnabled { get; private set; }
    public int LowScoreThreshold { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public NotificationPreference() { }

    public NotificationPreference(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        EmailEnabled = true;
        SignalREnabled = true;
        LowScoreThreshold = 400;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(bool emailEnabled, bool signalREnabled, int lowScoreThreshold)
    {
        EmailEnabled = emailEnabled;
        SignalREnabled = signalREnabled;
        LowScoreThreshold = lowScoreThreshold;
        UpdatedAt = DateTime.UtcNow;
    }
}